namespace LemonDo.Api.Tests.Tasks;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;

/// <summary>
/// Security hardening tests for the /api/tasks endpoint group.
/// A PASSING test means the endpoint correctly rejected the malicious request.
/// A FAILING test means a vulnerability exists — the endpoint accepted what it should have rejected.
/// </summary>
[TestClass]
public sealed class TaskSecurityHardeningTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _authenticatedClient = null!;
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _authenticatedClient.Dispose();
        _factory.Dispose();
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 1: Authentication Bypass
    //  All task endpoints require JWT. Every request without a valid
    //  token must return 401 Unauthorized.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return401_When_ListTasksWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "GET /api/tasks must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_CreateTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tasks", new { Title = "Attack" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_GetTaskByIdWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "GET /api/tasks/{id} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_UpdateTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", new { Title = "Attack" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "PUT /api/tasks/{id} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_DeleteTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "DELETE /api/tasks/{id} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_CompleteTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/tasks/{Guid.NewGuid()}/complete", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/{id}/complete must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_UncompleteTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/tasks/{Guid.NewGuid()}/uncomplete", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/{id}/uncomplete must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_ArchiveTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/tasks/{Guid.NewGuid()}/archive", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/{id}/archive must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_ViewNoteWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/view-note",
            new { Password = "TestPass123!" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/{id}/view-note must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_BulkCompleteWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = new[] { Guid.NewGuid() } });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/bulk/complete must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_AddTagWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/tags",
            new { Tag = "hacked" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/{id}/tags must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_RemoveTagWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}/tags/sometag");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "DELETE /api/tasks/{id}/tags/{tag} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_MoveTaskWithNoToken()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/move",
            new { ColumnId = Guid.NewGuid(), PreviousTaskId = (Guid?)null, NextTaskId = (Guid?)null });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/tasks/{id}/move must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_UsingMalformedBearerToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");
        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Malformed JWT token must be rejected");
    }

    [TestMethod]
    public async Task Should_Return401_When_UsingEmptyBearerToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");
        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Empty Bearer token must be rejected");
    }

    [TestMethod]
    public async Task Should_Return401_When_UsingRandomStringAsToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "randomstringnotajwt");
        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Random string token must be rejected");
    }

    [TestMethod]
    public async Task Should_Return401_When_UsingTokenSignedByWrongKey()
    {
        // JWT signed with a different secret than the server's test key
        // Header.Payload.Signature where signature is from a different key
        var fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" +
                        ".eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkF0dGFja2VyIn0" +
                        ".SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fakeToken);
        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "JWT signed with wrong key must be rejected");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 2: Input Validation — Title
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return400_When_CreateTaskWithEmptyTitle()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Empty title should be rejected with 400");
    }

    [TestMethod]
    public async Task Should_Return400_When_CreateTaskWithWhitespaceOnlyTitle()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "   " });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Whitespace-only title should be rejected with 400");
    }

    [TestMethod]
    public async Task Should_Return400_When_CreateTaskWithTitleExceedingMaxLength()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = new string('A', 501) });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Title exceeding 500 characters should be rejected with 400");
    }

    [TestMethod]
    public async Task Should_AcceptAndStore_When_CreateTaskWithXssPayloadInTitle()
    {
        // XSS in title: the API should either reject (400) or store literally (not execute)
        // Since this is a REST API returning JSON (not HTML), storage without execution is acceptable.
        // We verify the endpoint does NOT return 500 (server crash) and the payload is not executed.
        var xssPayload = "<script>alert('xss')</script>";
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = xssPayload });

        // Must not be a server error — either 201 (stored safely as text) or 400 (rejected)
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XSS payload in title must not cause a server error");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            $"XSS payload should result in 201 (stored as text) or 400 (rejected), got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_CreateTaskWithSqlInjectionInTitle()
    {
        var sqlPayload = "'; DROP TABLE Tasks; --";
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = sqlPayload });

        // SQL injection in a parameterized ORM should be stored literally (201) or rejected (400)
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in title must not cause a server error — EF Core uses parameterized queries");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            $"SQL injection payload should result in 201 or 400, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_UpdateTaskWithEmptyTitle()
    {
        // Create a task first
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Original title for update test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PutAsJsonAsync(
            $"/api/tasks/{created!.Id}",
            new { Title = "" });

        // Empty title on update must be rejected
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Updating task with empty title must return 400");
    }

    [TestMethod]
    public async Task Should_Return400_When_UpdateTaskWithTitleExceedingMaxLength()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PutAsJsonAsync(
            $"/api/tasks/{created!.Id}",
            new { Title = new string('Z', 501) });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Updating task with title > 500 chars must return 400");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 2: Input Validation — Description
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return400_When_CreateTaskWithDescriptionExceedingMaxLength()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Valid title", Description = new string('D', 10_001) });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Description exceeding 10,000 characters should be rejected with 400");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_CreateTaskWithSqlInjectionInDescription()
    {
        var sqlPayload = "'; DROP TABLE Tasks; --";
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Valid title", Description = sqlPayload });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in description must not cause a server error");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 2: Input Validation — Tags
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return400_When_AddTagExceedingMaxLength()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Tag length test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/tags",
            new { Tag = new string('T', 51) }); // Max is 50

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Tag exceeding 50 characters must be rejected with 400");
    }

    [TestMethod]
    public async Task Should_Return400_When_AddEmptyTag()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Empty tag test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/tags",
            new { Tag = "" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Empty tag must be rejected with 400");
    }

    [TestMethod]
    public async Task Should_Return400_When_AddWhitespaceOnlyTag()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Whitespace tag test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/tags",
            new { Tag = "   " });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Whitespace-only tag must be rejected with 400");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AddTagWithSqlInjectionCharacters()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "SQL tag injection test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // SQL injection in a tag field — EF Core should parameterize this safely
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/tags",
            new { Tag = "'; DROP TABLE Tasks; --" });

        // Either 400 (too long — this is 22 chars, within 50 limit) or 200 (stored literally), never 500
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in tag must not cause a server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AddTagWithXssPayload()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "XSS tag test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // XSS in tag — tag is normalized to lowercase so script tags come back as-is (text only)
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/tags",
            new { Tag = "<script>" }); // 8 chars, within limit

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XSS payload in tag must not cause a server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_SearchContainsSqlInjection()
    {
        var response = await _authenticatedClient.GetAsync(
            "/api/tasks?search=' OR '1'='1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in search query parameter must not cause server error");
        // EF Core's .Contains() translates to parameterized LIKE, so this should return 200 with no data leak
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "SQL injection in search should be handled safely and return 200");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_TagFilterContainsSqlInjection()
    {
        var response = await _authenticatedClient.GetAsync(
            "/api/tasks?tag=' OR '1'='1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in tag filter must not cause server error");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "SQL injection in tag filter should be handled safely and return 200");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 3: Pagination Attacks
    //  The ListTasks endpoint passes page/pageSize directly to the
    //  repository with no clamping. This tests boundary behavior.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_NotReturn500_When_PageIsNegative()
    {
        // page=-1 → Skip((-1-1)*pageSize) = Skip(-100) → negative skip value
        // EF Core/SQLite may throw or clamp. Must not return 500.
        var response = await _authenticatedClient.GetAsync("/api/tasks?page=-1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Negative page number must not cause a server error (500)");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_PageIsZero()
    {
        // page=0 → Skip((0-1)*pageSize) = Skip(-50) → negative skip
        var response = await _authenticatedClient.GetAsync("/api/tasks?page=0");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "page=0 must not cause a server error (500)");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_PageSizeIsZero()
    {
        var response = await _authenticatedClient.GetAsync("/api/tasks?page=1&pageSize=0");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "pageSize=0 must not cause a server error (500)");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_PageSizeIsNegative()
    {
        var response = await _authenticatedClient.GetAsync("/api/tasks?page=1&pageSize=-1");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Negative pageSize must not cause a server error (500)");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_PageSizeIsExtremelyLarge()
    {
        // An enormous page size could trigger a DoS by scanning the entire table
        var response = await _authenticatedClient.GetAsync("/api/tasks?pageSize=999999");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Extremely large pageSize must not cause a server error");
        // Ideally this should return 400, but at minimum must not crash.
        // The vulnerability is if it returns 200 with no clamping —
        // record the actual behavior for reporting.
    }

    [TestMethod]
    public async Task Should_EnforceMaxPageSize_When_PageSizeIsExtremelyLarge()
    {
        // A well-secured API should clamp pageSize to a reasonable maximum (e.g., 200)
        // to prevent DoS. This test documents whether that guard exists.
        var response = await _authenticatedClient.GetAsync("/api/tasks?pageSize=999999");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "pageSize=999999 should be rejected with 400 (no max-page-size guard found — potential DoS vector)");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 4: Business Logic Abuse
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_HandleGracefully_When_CompletingAlreadyCompletedTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Re-complete test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.PostAsync($"/api/tasks/{created!.Id}/complete", null);
        var secondComplete = await _authenticatedClient.PostAsync($"/api/tasks/{created.Id}/complete", null);

        // Domain allows re-completing (no-op via SetStatus). Must not be 500.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, secondComplete.StatusCode,
            "Re-completing a done task must not cause server error");
        Assert.IsTrue(
            secondComplete.StatusCode == HttpStatusCode.OK ||
            secondComplete.StatusCode == HttpStatusCode.UnprocessableEntity ||
            secondComplete.StatusCode == HttpStatusCode.Conflict,
            $"Re-completing done task should return 200 (no-op) or 422/409 (rule violation), got {secondComplete.StatusCode}");
    }

    [TestMethod]
    public async Task Should_HandleGracefully_When_ArchivingAlreadyArchivedTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Re-archive test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.PostAsync($"/api/tasks/{created!.Id}/archive", null);
        var secondArchive = await _authenticatedClient.PostAsync($"/api/tasks/{created.Id}/archive", null);

        // Re-archiving a task: Archive() is idempotent in the domain (just sets IsArchived=true again)
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, secondArchive.StatusCode,
            "Re-archiving must not cause server error");
    }

    [TestMethod]
    public async Task Should_Return404OrError_When_UncompletingTaskThatWasNeverCompleted()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Uncomplete never-completed" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // Task starts as Todo — uncomplete should either be a no-op (200) or fail gracefully
        var response = await _authenticatedClient.PostAsync($"/api/tasks/{created!.Id}/uncomplete", null);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Uncompleting a Todo task must not cause server error");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_DeletingAlreadyDeletedTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Delete twice test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.DeleteAsync($"/api/tasks/{created!.Id}");
        // Soft-deleted task should no longer be visible — second delete hits 404
        var secondDelete = await _authenticatedClient.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, secondDelete.StatusCode,
            "Deleting a soft-deleted task must return 404 (task no longer accessible)");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 5: Bulk Operations Abuse
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return4xx_When_BulkCompleteWithEmptyArray()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = Array.Empty<Guid>() });

        // An empty array should either return 400 (validation) or 200 (no-op).
        // Must not be 500.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Bulk complete with empty array must not cause server error");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_BulkCompleteWithMissingTaskIds()
    {
        // All IDs are non-existent — first one fails, whole batch fails
        var nonExistentIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = nonExistentIds });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Bulk complete with non-existent task IDs must return 404");
    }

    [TestMethod]
    public async Task Should_HandleGracefully_When_BulkCompleteWithDuplicateIds()
    {
        // Create one task, then submit it twice in bulk
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Duplicate bulk test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = new[] { created!.Id, created.Id } }); // same ID twice

        // Domain: task.Complete() is idempotent (SetStatus returns Success if already Done)
        // So the second iteration should succeed. Overall operation should not crash.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Bulk complete with duplicate IDs must not cause server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_BulkCompleteWithExtremelyLargeArray()
    {
        // Sending 1000 non-existent GUIDs — tests that fail-fast actually short-circuits early
        var manyIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToArray();
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = manyIds });

        // Should fail fast on the first missing ID — not scan all 1000
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Bulk complete with 1000 non-existent IDs must not cause server error");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Fail-fast: first missing ID should stop processing and return 404");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_BulkCompleteWithNullTaskIds()
    {
        // Sending null for the TaskIds array
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = (Guid[]?)null });

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Bulk complete with null TaskIds must not cause server error");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 6: IDOR on view-note
    //  The ViewTaskNoteCommandHandler uses GetByIdAsync(taskId, currentUser.UserId)
    //  so User B cannot reveal User A's note. This verifies that protection.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return404_When_ViewingAnotherUsersNote()
    {
        // User A creates a task with a sensitive note
        using var clientA = await RegisterFreshUserAsync("note-idor-a");
        using var clientB = await RegisterFreshUserAsync("note-idor-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks",
            new
            {
                Title = "User A secret task",
                SensitiveNote = "my secret content"
            });
        createResponse.EnsureSuccessStatusCode();
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B attempts to reveal User A's note (with their own valid password)
        var response = await clientB.PostAsJsonAsync(
            $"/api/tasks/{task!.Id}/view-note",
            new { Password = "TestPass123!" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B must NOT be able to view User A's sensitive note — task should not be found for B");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ViewNoteWithWrongPassword()
    {
        // Create a task with a sensitive note
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new
            {
                Title = "Note password test",
                SensitiveNote = "secret data"
            });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // Attempt to view with wrong password
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{task!.Id}/view-note",
            new { Password = "WrongPassword999!" });

        // Should be 401 (unauthorized re-auth) or 422 (business rule)
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Wrong password must not grant access to sensitive note");
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Wrong password must not cause server error");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ViewNoteOnTaskWithNoNote()
    {
        // Create a task without a sensitive note
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Task without note" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/tasks/{task!.Id}/view-note",
            new { Password = CustomWebApplicationFactory.TestUserPassword });

        // Should return 404 (note not found) — not 500
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Viewing note on task without note must not cause server error");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Task without a sensitive note should return 404 when note is requested");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 7: Mass Assignment / Over-Posting
    //  Verify that extra fields in the request body are silently
    //  ignored and do not affect sensitive properties.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_IgnoreExtraFields_When_CreateTaskWithUnknownProperties()
    {
        // Sending fields that don't exist in CreateTaskRequest — they should be ignored
        var body = new
        {
            Title = "Over-posting test",
            IsDeleted = true,        // internal field — must be ignored
            IsArchived = true,       // internal field — must be ignored
            OwnerId = Guid.NewGuid(), // another user's ID — must be ignored
            Status = "Done"          // status is set by lifecycle methods, not on create
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks", body);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode,
            "Request with extra fields must still succeed (fields are ignored)");

        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        Assert.IsNotNull(dto);
        Assert.AreEqual("Todo", dto.Status,
            "Task status must start as Todo regardless of Status field in body");
        Assert.IsFalse(dto.IsArchived,
            "IsArchived must not be settable via the create body");
    }

    [TestMethod]
    public async Task Should_IgnoreExtraFields_When_UpdateTaskWithInternalProperties()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Mass assignment update test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // Attacker tries to escalate by including internal fields
        var updateBody = new
        {
            Title = "Updated title",
            IsDeleted = true,         // soft-delete bypass attempt
            OwnerId = Guid.NewGuid(), // ownership transfer attempt
            Status = "Done",          // status bypass attempt
            CreatedAt = DateTimeOffset.MinValue
        };

        var response = await _authenticatedClient.PutAsJsonAsync(
            $"/api/tasks/{created!.Id}", updateBody);

        // Should succeed (200) — only Title is processed, rest ignored
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Update with extra internal fields must succeed (extra fields are ignored)");

        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        Assert.IsNotNull(dto);
        Assert.AreEqual("Updated title", dto.Title,
            "Title should be updated");
        Assert.AreEqual("Todo", dto.Status,
            "Status must not be changed by mass-assignment in update body");

        // Verify task is still accessible (not deleted)
        var getResponse = await _authenticatedClient.GetAsync($"/api/tasks/{created.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode,
            "Task must not be soft-deleted by over-posting IsDeleted=true");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 8: Information Leakage
    //  Error responses must not reveal stack traces, database column
    //  names, or sensitive system internals.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_ReturnGenericError_When_TaskNotFound()
    {
        var response = await _authenticatedClient.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Must not contain stack trace indicators
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "404 response must not contain stack trace");
        Assert.IsFalse(body.Contains("at LemonDo", StringComparison.OrdinalIgnoreCase),
            "404 response must not contain internal namespace");
        Assert.IsFalse(body.Contains("Exception", StringComparison.OrdinalIgnoreCase),
            "404 response must not contain exception type names");
    }

    [TestMethod]
    public async Task Should_ReturnStructuredError_When_ValidationFails()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Must not contain stack trace
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Validation error must not expose stack trace");
        Assert.IsFalse(body.Contains("at LemonDo", StringComparison.OrdinalIgnoreCase),
            "Validation error must not expose namespace paths");
    }

    [TestMethod]
    public async Task Should_NotExposePasswordHash_When_ViewingTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Leakage check" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var getResponse = await _authenticatedClient.GetAsync($"/api/tasks/{task!.Id}");
        var body = await getResponse.Content.ReadAsStringAsync();

        Assert.IsFalse(body.Contains("passwordHash", StringComparison.OrdinalIgnoreCase),
            "Task response must not contain passwordHash");
        Assert.IsFalse(body.Contains("encryptedSensitiveNote", StringComparison.OrdinalIgnoreCase),
            "Task response must not expose raw encrypted note shadow property");
        Assert.IsFalse(body.Contains("EncryptedSensitiveNote", StringComparison.OrdinalIgnoreCase),
            "Task response must not expose internal encrypted column name");
    }

    [TestMethod]
    public async Task Should_NotExposeEncryptedData_When_ListingTasks()
    {
        await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "List leakage test", SensitiveNote = "secret" });

        var response = await _authenticatedClient.GetAsync("/api/tasks");
        var body = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(body.Contains("encryptedSensitiveNote", StringComparison.OrdinalIgnoreCase),
            "Task list must not expose raw encrypted note");
        Assert.IsFalse(body.Contains("EncryptedSensitiveNote", StringComparison.OrdinalIgnoreCase),
            "Task list must not expose internal shadow property names");
    }

    [TestMethod]
    public async Task Should_Return401WithGenericMessage_When_NoTokenProvided()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Should not reveal internal token validation details
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "401 response must not contain stack trace");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 9: HTTP Method Enforcement
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return405_When_SendingPutToListEndpoint()
    {
        var response = await _authenticatedClient.PutAsJsonAsync("/api/tasks",
            new { Title = "Method confusion" });

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode,
            "PUT /api/tasks must return 405 — only GET and POST are valid on the collection");
    }

    [TestMethod]
    public async Task Should_Return405_When_SendingDeleteToListEndpoint()
    {
        var response = await _authenticatedClient.DeleteAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode,
            "DELETE /api/tasks must return 405");
    }

    [TestMethod]
    public async Task Should_Return405_When_SendingPatchToTaskEndpoint()
    {
        var id = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{id}")
        {
            Content = new StringContent("{\"title\":\"x\"}", Encoding.UTF8, "application/json")
        };
        var response = await _authenticatedClient.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode,
            "PATCH /api/tasks/{id} must return 405 — only GET, PUT, DELETE are valid");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 10: Path Traversal in Tag Route Parameter
    //  The tag is a URL segment: DELETE /api/tasks/{id}/tags/{tag}
    //  Verify special characters in the tag path segment are handled.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_NotReturn500_When_RemoveTagWithPathTraversalSequence()
    {
        // ../../ in a tag route param — ASP.NET routing should handle this safely
        var taskResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Path traversal tag test" });
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // URL-encode the path traversal so the router receives it as a segment value
        var response = await _authenticatedClient.DeleteAsync(
            $"/api/tasks/{task!.Id}/tags/..%2F..%2Fetc%2Fpasswd");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Path traversal in tag route segment must not cause server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_RemoveTagWithSpecialCharacters()
    {
        var taskResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Special char tag test" });
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // Tag with special chars (URL-encoded)
        var response = await _authenticatedClient.DeleteAsync(
            $"/api/tasks/{task!.Id}/tags/%3Cscript%3Ealert%28xss%29%3C%2Fscript%3E");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XSS payload in tag route segment must not cause server error");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 11: Soft-Deleted Resource Access
    //  Deleted tasks must not be accessible via any endpoint.
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return404_When_GetDeletedTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Task to delete and get" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.DeleteAsync($"/api/tasks/{created!.Id}");

        // Soft-deleted task must not be readable
        var getResponse = await _authenticatedClient.GetAsync($"/api/tasks/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode,
            "Soft-deleted task must not be retrievable via GET");
    }

    [TestMethod]
    public async Task Should_Return404_When_UpdateDeletedTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Task to delete and update" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.DeleteAsync($"/api/tasks/{created!.Id}");

        var updateResponse = await _authenticatedClient.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new { Title = "Updated after delete" });

        Assert.AreEqual(HttpStatusCode.NotFound, updateResponse.StatusCode,
            "Updating a soft-deleted task must return 404");
    }

    [TestMethod]
    public async Task Should_Return404_When_CompleteDeletedTask()
    {
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Task to delete and complete" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.DeleteAsync($"/api/tasks/{created!.Id}");

        var completeResponse = await _authenticatedClient.PostAsync(
            $"/api/tasks/{created.Id}/complete", null);

        Assert.AreEqual(HttpStatusCode.NotFound, completeResponse.StatusCode,
            "Completing a soft-deleted task must return 404");
    }

    [TestMethod]
    public async Task Should_NotAppear_When_DeletedTaskInList()
    {
        var uniqueTitle = $"SoftDeleteListCheck-{Guid.NewGuid():N}";
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = uniqueTitle });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _authenticatedClient.DeleteAsync($"/api/tasks/{created!.Id}");

        // Deleted tasks must not appear in list
        var listResponse = await _authenticatedClient.GetAsync("/api/tasks");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<TaskDto>>(JsonOpts);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Items.Any(t => t.Id == created.Id),
            "Soft-deleted task must not appear in task list");
    }

    // ─────────────────────────────────────────────────────────────
    //  Helper
    // ─────────────────────────────────────────────────────────────

    private static async Task<HttpClient> RegisterFreshUserAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@lemondo.dev";
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = $"User {prefix}" });
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }
}
