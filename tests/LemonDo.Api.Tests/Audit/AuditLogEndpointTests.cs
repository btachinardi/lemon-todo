namespace LemonDo.Api.Tests.Audit;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Common;

[TestClass]
public sealed class AuditLogEndpointTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    public async Task Should_Return401_When_UnauthenticatedUserAccessesAuditLog()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/audit");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return403_When_RegularUserAccessesAuditLog()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/admin/audit");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return200_When_AdminAccessesAuditLog()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/admin/audit");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task Should_ReturnAuditEntries_When_TaskIsCreatedAndLogSearched()
    {
        // Create a task to generate audit entries
        var taskClient = await _factory.CreateAuthenticatedClientAsync();
        await taskClient.PostAsJsonAsync("/api/tasks", new { Title = "Audit Log Test Task" });

        // Search audit log as admin
        var adminClient = await _factory.CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/audit?resourceType=Task");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.TotalCount);
    }

    [TestMethod]
    public async Task Should_FilterByAction_When_ActionParameterProvided()
    {
        var client = await _factory.CreateAdminClientAsync();

        // Filter for task creation events specifically
        var response = await client.GetAsync("/api/admin/audit?action=TaskCreated");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(TestJsonOptions.Default);
        Assert.IsNotNull(result);

        // All returned entries should have TaskCreated action
        foreach (var entry in result.Items)
        {
            Assert.AreEqual("TaskCreated", entry.Action.ToString());
        }
    }
}
