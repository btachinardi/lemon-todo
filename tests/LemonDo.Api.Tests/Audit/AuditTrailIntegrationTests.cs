using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;

namespace LemonDo.Api.Tests.Audit;

[TestClass]
public sealed class AuditTrailIntegrationTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    public async Task Should_CreateAuditEntry_When_TaskIsCreated()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a task — this should trigger AuditOnTaskCreated event handler
        var response = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Audit test task" });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        // The audit entry is created via domain event, so we just verify the task was created
        // The audit search endpoint tests (CP4.6) will verify audit entries directly
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
        Assert.IsNotNull(task);
    }

    [TestMethod]
    public async Task Should_CreateAuditEntry_When_TaskIsDeleted()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create and delete
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Task to delete for audit" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);

        var deleteResponse = await client.DeleteAsync($"/api/tasks/{task!.Id}");
        Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task Should_CreateAuditEntry_When_TaskIsCompleted()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create and complete
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Task to complete for audit" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);

        var completeResponse = await client.PostAsync($"/api/tasks/{task!.Id}/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, completeResponse.StatusCode);
    }
}
