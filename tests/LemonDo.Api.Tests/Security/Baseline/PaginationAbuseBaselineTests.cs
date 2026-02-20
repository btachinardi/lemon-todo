namespace LemonDo.Api.Tests.Security.Baseline;

using System.Net;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Baseline pagination abuse tests for all paginated endpoints.
/// Verifies that abusive page/pageSize values do not crash the server.
/// </summary>
[TestClass]
public sealed class PaginationAbuseBaselineTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _userClient = null!;
    private static HttpClient _adminClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _userClient = await _factory.CreateAuthenticatedClientAsync();
        _adminClient = await _factory.CreateAdminClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _userClient.Dispose();
        _adminClient.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    [DynamicData(nameof(PaginationTestData.AbusiveValues), typeof(PaginationTestData))]
    public async Task Should_NotCrash_When_AbusivePaginationOnTasks(int page, int pageSize, string desc)
    {
        var response = await _userClient.GetAsync($"/api/tasks?page={page}&pageSize={pageSize}");
        SecurityAssertions.AssertNotServerError(response, $"GET /api/tasks [{desc}]");
    }

    [TestMethod]
    [DynamicData(nameof(PaginationTestData.AbusiveValues), typeof(PaginationTestData))]
    public async Task Should_NotCrash_When_AbusivePaginationOnNotifications(int page, int pageSize, string desc)
    {
        var response = await _userClient.GetAsync($"/api/notifications?page={page}&pageSize={pageSize}");
        SecurityAssertions.AssertNotServerError(response, $"GET /api/notifications [{desc}]");
    }

    [TestMethod]
    [DynamicData(nameof(PaginationTestData.AbusiveValues), typeof(PaginationTestData))]
    public async Task Should_NotCrash_When_AbusivePaginationOnAdminUsers(int page, int pageSize, string desc)
    {
        var response = await _adminClient.GetAsync($"/api/admin/users?page={page}&pageSize={pageSize}");
        SecurityAssertions.AssertNotServerError(response, $"GET /api/admin/users [{desc}]");
    }

    [TestMethod]
    [DynamicData(nameof(PaginationTestData.AbusiveValues), typeof(PaginationTestData))]
    public async Task Should_NotCrash_When_AbusivePaginationOnAuditLog(int page, int pageSize, string desc)
    {
        var response = await _adminClient.GetAsync($"/api/admin/audit?page={page}&pageSize={pageSize}");
        SecurityAssertions.AssertNotServerError(response, $"GET /api/admin/audit [{desc}]");
    }
}
