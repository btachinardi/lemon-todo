namespace LemonDo.Api.Tests.Events;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class DomainEventDispatcherTests
{
    [TestMethod]
    public async Task Should_DispatchEvents_When_HandlersRegistered()
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

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Event dispatch test" });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsTrue(capturedEvents.Count > 0, "Expected at least one domain event to be dispatched.");
        Assert.IsInstanceOfType<TaskCreatedEvent>(capturedEvents[0]);
        Assert.AreEqual("Event dispatch test", ((TaskCreatedEvent)capturedEvents[0]).Title);
    }

    [TestMethod]
    public async Task Should_NotThrow_When_NoHandlersRegistered()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "No handler test" });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
        Assert.IsNotNull(dto);
        Assert.AreEqual("No handler test", dto.Title);
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
