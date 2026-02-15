namespace LemonDo.Api.Tests.Transactions;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class TransactionAtomicityTests
{
    [TestMethod]
    public async Task Should_NotPersistTask_When_EventHandlerThrows()
    {
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDomainEventHandler<TaskCreatedEvent>>(
                    _ => new ThrowingHandler<TaskCreatedEvent>());
            });
        });

        using var client = await factory.CreateAuthenticatedClientAsync();

        // Get initial task count
        var listBefore = await client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks", TestJsonOptions.Default);
        Assert.IsNotNull(listBefore);
        var initialCount = listBefore.TotalCount;

        // POST should fail because the event handler throws
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Rollback task test" });
        Assert.AreEqual(HttpStatusCode.InternalServerError, createResponse.StatusCode);

        // Task should NOT be persisted — the transaction must have rolled back
        var listAfter = await client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks", TestJsonOptions.Default);
        Assert.IsNotNull(listAfter);
        Assert.AreEqual(initialCount, listAfter.TotalCount,
            "Task count should not increase when event handler throws.");
    }

    [TestMethod]
    public async Task Should_NotPersistBoardCard_When_EventHandlerThrows()
    {
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDomainEventHandler<TaskCreatedEvent>>(
                    _ => new ThrowingHandler<TaskCreatedEvent>());
            });
        });

        using var client = await factory.CreateAuthenticatedClientAsync();

        // Get initial board card count
        var boardBefore = await client.GetFromJsonAsync<BoardDto>("/api/boards/default", TestJsonOptions.Default);
        Assert.IsNotNull(boardBefore);
        var initialCardCount = boardBefore.Cards?.Count ?? 0;

        // POST should fail because the event handler throws
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Board-card rollback test" });
        Assert.AreEqual(HttpStatusCode.InternalServerError, createResponse.StatusCode);

        // Board should have the same number of cards as before — no phantom card
        var boardAfter = await client.GetFromJsonAsync<BoardDto>("/api/boards/default", TestJsonOptions.Default);
        Assert.IsNotNull(boardAfter);
        var afterCardCount = boardAfter.Cards?.Count ?? 0;

        Assert.AreEqual(initialCardCount, afterCardCount,
            "Board should not have an extra card when event handler throws.");
    }

    [TestMethod]
    public async Task Should_RollbackAllChanges_When_SecondEventHandlerThrows()
    {
        var capturedEvents = new List<DomainEvent>();

        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // First handler captures the event successfully
                services.AddScoped<IDomainEventHandler<TaskCreatedEvent>>(
                    _ => new CapturingHandler<TaskCreatedEvent>(capturedEvents));
                // Second handler throws — should cause full rollback
                services.AddScoped<IDomainEventHandler<TaskCreatedEvent>>(
                    _ => new ThrowingHandler<TaskCreatedEvent>());
            });
        });

        using var client = await factory.CreateAuthenticatedClientAsync();

        // Get initial task count
        var listBefore = await client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks", TestJsonOptions.Default);
        Assert.IsNotNull(listBefore);
        var initialCount = listBefore.TotalCount;

        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Multi-handler rollback test" });
        Assert.AreEqual(HttpStatusCode.InternalServerError, createResponse.StatusCode);

        // The capturing handler DID fire (events were dispatched before the throw)
        Assert.IsNotEmpty(capturedEvents, "First handler should have received the event.");

        // But the task should NOT be in the database — full rollback
        var listAfter = await client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks", TestJsonOptions.Default);
        Assert.IsNotNull(listAfter);
        Assert.AreEqual(initialCount, listAfter.TotalCount,
            "Task should not be persisted when any event handler throws.");
    }

    [TestMethod]
    public async Task Should_PersistTask_When_NoEventHandlerRegistered()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "No handler control test" });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var dto = await createResponse.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
        Assert.IsNotNull(dto);

        var getResponse = await client.GetAsync($"/api/tasks/{dto.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [TestMethod]
    public async Task Should_PersistTask_When_EventHandlerSucceeds()
    {
        var capturedEvents = new List<DomainEvent>();

        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDomainEventHandler<TaskCreatedEvent>>(
                    _ => new CapturingHandler<TaskCreatedEvent>(capturedEvents));
            });
        });

        using var client = await factory.CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Successful handler test" });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var dto = await createResponse.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
        Assert.IsNotNull(dto);

        // Events were dispatched
        Assert.IsNotEmpty(capturedEvents);

        // Task persisted
        var getResponse = await client.GetAsync($"/api/tasks/{dto.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
    }

    private sealed class ThrowingHandler<TEvent> : IDomainEventHandler<TEvent>
        where TEvent : DomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated event handler failure for transaction test.");
        }
    }

    private sealed class CapturingHandler<TEvent>(List<DomainEvent> captured) : IDomainEventHandler<TEvent>
        where TEvent : DomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken ct = default)
        {
            captured.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
