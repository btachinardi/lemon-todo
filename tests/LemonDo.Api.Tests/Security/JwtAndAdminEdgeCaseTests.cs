namespace LemonDo.Api.Tests.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Administration.DTOs;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Security hardening tests — Pass 6: JWT edge cases, admin self-operations,
/// cross-context data leakage, audit log security, and deactivated-user consistency.
///
/// A PASSING test = the endpoint correctly rejected or safely handled the attack vector.
/// A FAILING test = a vulnerability exists — document the actual vs. expected behavior.
///
/// ISOLATION STRATEGY:
/// - Read-only tests use the shared _factory (no side effects).
/// - Destructive tests (deactivate, remove role, etc.) use private factory instances
///   created and disposed within the test to avoid contaminating the shared seeded accounts.
/// </summary>
[TestClass]
public sealed class JwtAndAdminEdgeCaseTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    // ============================================================
    // HELPERS
    // ============================================================

    /// <summary>
    /// Creates a fresh HttpClient with no cookie handling — prevents cross-test
    /// refresh-token cookie contamination.
    /// </summary>
    private HttpClient CreateFreshClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    /// <summary>
    /// Registers a brand-new user against the shared factory and returns an authenticated client + email.
    /// Uses a unique email each call so tests are isolated.
    /// </summary>
    private static async Task<(HttpClient Client, string Email)> RegisterFreshUserAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@lemondo.dev";
        const string password = "TestPass123!";
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = password, DisplayName = $"User {prefix}" });
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return (client, email);
    }

    /// <summary>
    /// Creates an isolated factory with its own in-memory database.
    /// Use for destructive tests (deactivate, role removal) that would contaminate
    /// shared seeded accounts if run against _factory.
    /// The caller is responsible for disposing the returned factory.
    /// </summary>
    private static CustomWebApplicationFactory CreateIsolatedFactory() => new();

    /// <summary>
    /// Finds a user by email via the sysadmin user-list endpoint and returns their user ID.
    /// </summary>
    private static async Task<Guid> FindUserIdByEmailAsync(HttpClient sysAdminClient, string email)
    {
        var response = await sysAdminClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(email)}&pageSize=1");
        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserDto>>(JsonOpts);
        Assert.IsNotNull(list);
        Assert.IsNotEmpty(list.Items, $"Could not find user with email {email}");
        return list.Items[0].Id;
    }

    /// <summary>
    /// Extracts the raw "refresh_token=..." cookie value from a login/register response.
    /// Returns empty string if not present.
    /// </summary>
    private static string ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return string.Empty;
        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        if (setCookie is null) return string.Empty;
        var semiIndex = setCookie.IndexOf(';');
        return semiIndex > 0 ? setCookie[..semiIndex] : setCookie;
    }

    // ============================================================
    // CATEGORY 1: JWT Token Edge Cases
    // ============================================================

    [TestMethod]
    public async Task Should_Return401_When_TokenHasExpiredExpClaim()
    {
        // Craft a structurally valid JWT with exp = now - 1 hour, signed with the test key.
        // ClockSkew is zero, so even 1 second past expiry must be rejected.
        using var client = CreateFreshClient();

        var expiredToken = BuildSignedToken(
            sub: Guid.NewGuid().ToString(),
            exp: DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        var response = await client.GetAsync("/api/auth/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Expired JWT (past exp claim) must be rejected with 401 — ClockSkew=Zero must be enforced");
    }

    [TestMethod]
    public async Task Should_Return401_When_TokenHasFutureNbfClaim()
    {
        // Craft a JWT where nbf (not-before) is set to 1 hour in the future.
        // The token is otherwise valid (future exp), but must be rejected because
        // it is not yet valid. ASP.NET JWT handler respects nbf by default.
        using var client = CreateFreshClient();

        var futureNbf = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var futureExp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
        var token = BuildSignedToken(
            sub: Guid.NewGuid().ToString(),
            exp: futureExp,
            extraClaims: $",\"nbf\":{futureNbf}");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Token with future nbf claim must be rejected — token is not yet valid");
    }

    [TestMethod]
    public async Task Should_Return401_When_ValidJwtButUserDoesNotExistInDatabase()
    {
        // A valid JWT (correct sig, not expired) for a user ID that doesn't exist in the DB.
        // GET /api/auth/me calls userRepository.GetByIdAsync() and returns 401 if user is null.
        using var client = CreateFreshClient();

        var nonExistentUserId = Guid.NewGuid();
        var token = BuildSignedToken(sub: nonExistentUserId.ToString());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "A valid JWT for a non-existent user must return 401 — GetMe verifies user exists in DB");
    }

    [TestMethod]
    public async Task Should_Return401_When_TokenHasEmptySubClaim()
    {
        // Craft a JWT where the 'sub' (NameIdentifier) claim is present but empty ("").
        // GetMe parses the sub claim: Guid.TryParse("") returns false → should return 401.
        // KNOWN BUG: Empty sub currently causes a 500 — this test documents the vulnerability.
        using var client = CreateFreshClient();

        var token = BuildSignedToken(sub: string.Empty);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        // NOTE: If this returns 500, the ErrorHandlingMiddleware is not catching the exception
        // thrown when the GUID parse fails and passes null to UserId.Reconstruct.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "VULNERABILITY: Token with empty 'sub' claim causes a 500 Internal Server Error. " +
            "The GetMe handler does: Guid.TryParse(userIdStr, out var guid) — if this returns false " +
            "it correctly returns 401. But if the sub claim is empty, the NameIdentifier ClaimType " +
            "might still be populated, causing UserId.Reconstruct(Guid.Empty) to throw. " +
            "Fix: validate that guid != Guid.Empty before calling UserId.Reconstruct.");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Token with empty 'sub' claim must return 401, not 500");
    }

    [TestMethod]
    public async Task Should_Return401_When_TokenHasMissingSubClaim()
    {
        // Craft a JWT with no 'sub' claim at all (missing, not empty).
        // ClaimTypes.NameIdentifier will be null → GetMe should return 401.
        using var client = CreateFreshClient();

        // Build a token with no sub claim — only jti, iss, aud, exp
        var secretKey = "test-secret-key-at-least-32-characters-long!!";
        var futureExp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payloadJson = $"{{\"jti\":\"{Guid.NewGuid()}\",\"iss\":\"LemonDo\",\"aud\":\"LemonDo\",\"exp\":{futureExp}}}";
        var token = SignPayload(payloadJson, secretKey);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Token with missing 'sub' claim must return 401 — no user ID to look up");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_VeryLongAuthorizationHeader()
    {
        // A 50,000-character Bearer token — server must not crash or OOM.
        using var client = CreateFreshClient();

        var hugeToken = "Bearer " + new string('A', 50_000);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", hugeToken);

        var response = await client.GetAsync("/api/auth/me");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "A 50,000-character Authorization header must not crash the server");
        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"50k-char auth header must produce a 4xx response, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return401_When_ValidTokenButSubIsNotAGuid()
    {
        // Craft a JWT where 'sub' is a non-GUID string (e.g., "admin").
        // GetMe: Guid.TryParse("admin") returns false → should return 401.
        // KNOWN BUG: Same as empty sub — may cause 500 if the code reaches UserId.Reconstruct
        // with a zero GUID from a failed TryParse.
        using var client = CreateFreshClient();

        var token = BuildSignedToken(sub: "admin");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "VULNERABILITY: Token with non-GUID 'sub' claim causes a 500. " +
            "Fix: add Guid.Empty check before UserId.Reconstruct in GetMe.");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Token with non-GUID 'sub' claim must return 401, not 500");
    }

    [TestMethod]
    public async Task Should_Return401_When_TokenHasWrongIssuer()
    {
        // Token signed with the correct key but wrong issuer — ValidateIssuer=true must reject it.
        using var client = CreateFreshClient();

        var secretKey = "test-secret-key-at-least-32-characters-long!!";
        var futureExp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payloadJson = $"{{\"sub\":\"{Guid.NewGuid()}\",\"jti\":\"{Guid.NewGuid()}\",\"iss\":\"EvilIssuer\",\"aud\":\"LemonDo\",\"exp\":{futureExp}}}";
        var token = SignPayload(payloadJson, secretKey);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Token with wrong issuer must be rejected even if correctly signed — ValidateIssuer=true");
    }

    [TestMethod]
    public async Task Should_Return401_When_TokenHasWrongAudience()
    {
        // Token signed with the correct key but wrong audience — ValidateAudience=true must reject it.
        using var client = CreateFreshClient();

        var secretKey = "test-secret-key-at-least-32-characters-long!!";
        var futureExp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payloadJson = $"{{\"sub\":\"{Guid.NewGuid()}\",\"jti\":\"{Guid.NewGuid()}\",\"iss\":\"LemonDo\",\"aud\":\"EvilAudience\",\"exp\":{futureExp}}}";
        var token = SignPayload(payloadJson, secretKey);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Token with wrong audience must be rejected even if correctly signed — ValidateAudience=true");
    }

    // ============================================================
    // CATEGORY 2: Admin Self-Operations
    // Each test uses an ISOLATED factory to prevent modifying the shared sysadmin account.
    // ============================================================

    [TestMethod]
    public async Task Should_Prevent_When_SystemAdminDeactivatesOwnAccount()
    {
        // VULNERABILITY TEST: A SystemAdmin calling deactivate on their own user ID.
        // Expected: endpoint should reject self-deactivation (4xx).
        // Uses isolated factory so that the shared sysadmin is not affected.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();
        var sysAdminId = await FindUserIdByEmailAsync(sysAdminClient, CustomWebApplicationFactory.SystemAdminUserEmail);

        var response = await sysAdminClient.PostAsync($"/api/admin/users/{sysAdminId}/deactivate", null);

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "VULNERABILITY CONFIRMED: SystemAdmin was able to deactivate their own account (returned 200). " +
            "Self-deactivation should be rejected to prevent admin lockout. " +
            "Fix: add a self-deactivation guard in DeactivateUserAsync or DeactivateUserCommandHandler " +
            "that compares command.UserId to requestContext.UserId.");

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Self-deactivation must return 4xx, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Prevent_When_SystemAdminRemovesOwnSystemAdminRole()
    {
        // VULNERABILITY TEST: A SystemAdmin removing their own SystemAdmin role.
        // If this succeeds, the calling admin silently loses privileges on the next token refresh.
        // Uses isolated factory.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();
        var sysAdminId = await FindUserIdByEmailAsync(sysAdminClient, CustomWebApplicationFactory.SystemAdminUserEmail);

        var response = await sysAdminClient.DeleteAsync($"/api/admin/users/{sysAdminId}/roles/SystemAdmin");

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "VULNERABILITY: SystemAdmin was able to remove their own SystemAdmin role. " +
            "This silently de-escalates privileges on the next token refresh. " +
            "Fix: block self-role-removal for the calling user's own privileged roles.");

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Self SystemAdmin-role removal must return 4xx, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Prevent_When_SystemAdminRemovesOwnUserRole()
    {
        // VULNERABILITY TEST: A SystemAdmin removing the base "User" role from themselves.
        // Uses isolated factory.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();
        var sysAdminId = await FindUserIdByEmailAsync(sysAdminClient, CustomWebApplicationFactory.SystemAdminUserEmail);

        var response = await sysAdminClient.DeleteAsync($"/api/admin/users/{sysAdminId}/roles/User");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Self User-role removal must not crash the server (500)");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Fail("FINDING: SystemAdmin was able to remove their own 'User' base role. " +
                "This may break task/board access if those endpoints implicitly require the User role. " +
                "Recommendation: block self-role-removal, especially for base roles.");
        }
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminCallsRevealProtectedDataOnOwnAccount()
    {
        // Admin (not SystemAdmin) calling the reveal endpoint on their own user ID.
        // The reveal endpoint requires SystemAdmin — Admin should get 403.
        // Uses shared factory — read-only for the admin's permissions.
        var adminClient = await _factory.CreateAdminClientAsync();
        // Look up admin ID via the shared sysadmin
        var sysAdminClient = await _factory.CreateSystemAdminClientAsync();
        var adminId = await FindUserIdByEmailAsync(sysAdminClient, CustomWebApplicationFactory.AdminUserEmail);

        var response = await adminClient.PostAsJsonAsync($"/api/admin/users/{adminId}/reveal",
            new { Reason = "SupportTicket", Password = CustomWebApplicationFactory.AdminUserPassword });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
            "Admin (non-SystemAdmin) must get 403 when calling the SystemAdmin-only reveal endpoint, " +
            "even if targeting their own account");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_SystemAdminReactivatesAlreadyActiveAccount()
    {
        // Reactivating an already-active account (themselves) must fail — domain business rule.
        var sysAdminClient = await _factory.CreateSystemAdminClientAsync();
        var sysAdminId = await FindUserIdByEmailAsync(sysAdminClient, CustomWebApplicationFactory.SystemAdminUserEmail);

        var response = await sysAdminClient.PostAsync($"/api/admin/users/{sysAdminId}/reactivate", null);

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Reactivating an already-active account must fail (domain business rule: not deactivated)");
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Business rule violation must not cause a 500 — must return a structured 4xx error");
    }

    // ============================================================
    // CATEGORY 3: Cross-Context Data Leakage (Task + Board)
    // ============================================================

    [TestMethod]
    public async Task Should_Return4xx_When_MoveTaskToColumnBelongingToAnotherUsersBoard()
    {
        // User A creates a task and tries to move it to a column that belongs to User B's board.
        // Expected: 4xx — the column doesn't exist on A's board (domain error from board.MoveCard).
        var (clientA, _) = await RegisterFreshUserAsync("cross-ctx-move-a");
        var (clientB, _) = await RegisterFreshUserAsync("cross-ctx-move-b");

        // Get User B's board columns
        var boardBResponse = await clientB.GetAsync("/api/boards/default");
        boardBResponse.EnsureSuccessStatusCode();
        var boardB = await boardBResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        Assert.IsNotNull(boardB);
        Assert.IsNotEmpty(boardB.Columns, "User B's board must have at least one column");
        var columnFromBoardB = boardB.Columns[0].Id;

        // User A creates a task
        var createResponse = await clientA.PostAsJsonAsync("/api/tasks",
            new { Title = "Cross-context move victim" });
        createResponse.EnsureSuccessStatusCode();
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User A tries to move their own task to User B's column
        var moveResponse = await clientA.PostAsJsonAsync($"/api/tasks/{task!.Id}/move",
            new { ColumnId = columnFromBoardB, PreviousTaskId = (Guid?)null, NextTaskId = (Guid?)null });

        Assert.IsTrue(
            (int)moveResponse.StatusCode >= 400 && (int)moveResponse.StatusCode < 500,
            $"Moving a task to another user's column must be rejected with 4xx, got {(int)moveResponse.StatusCode}. " +
            "If 200: User A's task card is being placed on User B's board — a cross-context data leak.");
    }

    [TestMethod]
    public async Task Should_Return404_When_UserBTriesToMoveUserAsTask()
    {
        // User B tries to move User A's task. GetByIdAsync scopes by owner → task not found for B → 404.
        var (clientA, _) = await RegisterFreshUserAsync("cross-ctx-steal-a");
        var (clientB, _) = await RegisterFreshUserAsync("cross-ctx-steal-b");

        // User A creates a task
        var createResponse = await clientA.PostAsJsonAsync("/api/tasks",
            new { Title = "User A's task — B should not be able to move this" });
        createResponse.EnsureSuccessStatusCode();
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // Get User B's own board columns (valid target column)
        var boardBResponse = await clientB.GetAsync("/api/boards/default");
        boardBResponse.EnsureSuccessStatusCode();
        var boardB = await boardBResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        var columnFromBoardB = boardB!.Columns[0].Id;

        // User B tries to move User A's task to their own column
        var moveResponse = await clientB.PostAsJsonAsync($"/api/tasks/{task!.Id}/move",
            new { ColumnId = columnFromBoardB, PreviousTaskId = (Guid?)null, NextTaskId = (Guid?)null });

        Assert.AreEqual(HttpStatusCode.NotFound, moveResponse.StatusCode,
            "User B must not be able to move User A's task — task repository scopes by owner → 404");
    }

    [TestMethod]
    public async Task Should_ReturnDifferentBoardIds_When_TwoFreshUsersGetDefaultBoard()
    {
        // Board isolation: each user has their own default board — they must not share one.
        var (clientA, _) = await RegisterFreshUserAsync("board-iso-a");
        var (clientB, _) = await RegisterFreshUserAsync("board-iso-b");

        var boardAResponse = await clientA.GetAsync("/api/boards/default");
        boardAResponse.EnsureSuccessStatusCode();
        var boardA = await boardAResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        var boardBResponse = await clientB.GetAsync("/api/boards/default");
        boardBResponse.EnsureSuccessStatusCode();
        var boardB = await boardBResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        Assert.IsNotNull(boardA);
        Assert.IsNotNull(boardB);
        Assert.AreNotEqual(boardA.Id, boardB.Id,
            "Two different users must have different default board IDs — board isolation failed");
    }

    [TestMethod]
    public async Task Should_NotLeakTasksAcrossUsers_When_TaskCreatedByUserA()
    {
        // User B's task list must never include User A's tasks.
        var (clientA, _) = await RegisterFreshUserAsync("task-iso-a");
        var (clientB, _) = await RegisterFreshUserAsync("task-iso-b");

        var uniqueTitle = $"UserA-Private-{Guid.NewGuid():N}";
        await clientA.PostAsJsonAsync("/api/tasks", new { Title = uniqueTitle });

        var listResponse = await clientB.GetAsync("/api/tasks");
        listResponse.EnsureSuccessStatusCode();
        var body = await listResponse.Content.ReadAsStringAsync();

        Assert.IsFalse(body.Contains(uniqueTitle, StringComparison.Ordinal),
            "User B's task list must not contain User A's tasks — user isolation failed");
    }

    [TestMethod]
    public async Task Should_Return404_When_UserBTriesToViewUserAsNote()
    {
        // IDOR on sensitive note: User B tries to view User A's task note.
        // Task lookup is scoped by owner → 404 before password verification.
        var (clientA, _) = await RegisterFreshUserAsync("note-idor-a");
        var (clientB, _) = await RegisterFreshUserAsync("note-idor-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks",
            new { Title = "IDOR note test task", SensitiveNote = "very secret content" });
        createResponse.EnsureSuccessStatusCode();
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var noteResponse = await clientB.PostAsJsonAsync($"/api/tasks/{task!.Id}/view-note",
            new { Password = "TestPass123!" });

        Assert.AreEqual(HttpStatusCode.NotFound, noteResponse.StatusCode,
            "User B must not be able to view User A's note — task is not found for User B");
    }

    // ============================================================
    // CATEGORY 4: Audit Log Security
    // ============================================================

    [TestMethod]
    public async Task Should_NotCrash_When_AuditLogDateFromHasSqlInjection()
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        var response = await adminClient.GetAsync(
            $"/api/admin/audit?dateFrom={Uri.EscapeDataString("'; DROP TABLE AuditEntries; --")}");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in dateFrom must not crash the server");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"SQL injection in dateFrom should result in 200 (param null/ignored) or 400, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AuditLogDateFromIsInvalidFormat()
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/audit?dateFrom=not-a-date");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Invalid dateFrom format must not crash the server");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Invalid dateFrom must result in 200 (param ignored) or 400, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_NotCrash_When_AuditLogActionIsInvalidEnumValue()
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/audit?action=HackerAction");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Invalid AuditAction enum value must not crash the server");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Invalid AuditAction must result in 200 or 400, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_RecordAuditEntry_When_UserIsDeactivated()
    {
        // Positive test: after deactivating a user, an audit entry must exist.
        // Uses isolated factory — the deactivation happens on a fresh isolated DB.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();

        // Register a fresh user in the isolated factory
        var email = $"audit-deactivate-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = isolatedFactory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Audit Deactivate Test" });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        var userId = auth!.User.Id;

        // Deactivate the user
        var deactivateResponse = await sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, deactivateResponse.StatusCode, "Deactivation should succeed");

        // Check the audit log for the deactivation entry
        var auditResponse = await sysAdminClient.GetAsync(
            $"/api/admin/audit?action=UserDeactivated&resourceType=User&pageSize=10");
        Assert.AreEqual(HttpStatusCode.OK, auditResponse.StatusCode);

        var auditResult = await auditResponse.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(JsonOpts);
        Assert.IsNotNull(auditResult);

        var deactivationEntry = auditResult.Items.FirstOrDefault(
            e => e.ResourceId == userId.ToString());

        Assert.IsNotNull(deactivationEntry,
            $"Audit log must contain a UserDeactivated entry for user {userId}. " +
            $"Total entries found: {auditResult.Items.Count}. " +
            "This verifies the audit trail is functioning correctly.");
    }

    [TestMethod]
    public async Task Should_RecordAuditEntry_When_SystemAdminRevealsProtectedData()
    {
        // Positive test: after a successful reveal, an audit entry for ProtectedDataRevealed must exist.
        // Uses isolated factory to keep audit entries isolated from other tests.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();
        var sysAdminId = await FindUserIdByEmailAsync(sysAdminClient, CustomWebApplicationFactory.SystemAdminUserEmail);

        // Reveal the sysadmin's own protected data
        var revealResponse = await sysAdminClient.PostAsJsonAsync($"/api/admin/users/{sysAdminId}/reveal",
            new
            {
                Reason = "SupportTicket",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });
        Assert.AreEqual(HttpStatusCode.OK, revealResponse.StatusCode,
            "Reveal operation must succeed with correct password and valid reason");

        // Check the audit log for the reveal entry
        var auditResponse = await sysAdminClient.GetAsync(
            $"/api/admin/audit?action=ProtectedDataRevealed&resourceType=User&pageSize=20");
        Assert.AreEqual(HttpStatusCode.OK, auditResponse.StatusCode);

        var auditResult = await auditResponse.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(JsonOpts);
        Assert.IsNotNull(auditResult);

        Assert.IsGreaterThan(0, auditResult.TotalCount,
            "At least one ProtectedDataRevealed audit entry must exist after a successful reveal. " +
            "Audit trail integrity is a compliance requirement.");
    }

    [TestMethod]
    public async Task Should_NotExposeStackTrace_When_AuditLogInvalidDateRange()
    {
        // dateFrom > dateTo — should be handled gracefully (empty result or 400), never 500 with stack trace.
        var adminClient = await _factory.CreateAdminClientAsync();
        var future = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddYears(1).ToString("O"));
        var past = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddYears(-1).ToString("O"));
        var response = await adminClient.GetAsync($"/api/admin/audit?dateFrom={future}&dateTo={past}");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Inverted date range (dateFrom > dateTo) must not crash the server");

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Audit log response must not expose stack traces for invalid date ranges");
    }

    // ============================================================
    // CATEGORY 5: Rate Limiting Wiring Verification
    // ============================================================

    [TestMethod]
    public async Task Should_Succeed_When_AuthEndpointCalledWithValidCredentials()
    {
        // Verify rate limiter is wired but not blocking valid requests.
        // In test config PermitLimit=10000 — legitimate requests must succeed.
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Auth endpoint must return 200 with valid credentials — rate limiter is active but under limit");
        Assert.AreNotEqual(HttpStatusCode.TooManyRequests, response.StatusCode,
            "Auth endpoint must not return 429 when under the configured rate limit (10000/min in tests)");
    }

    // ============================================================
    // CATEGORY 6: Deactivated User — Consistent 401 Across All Endpoint Types
    // Uses isolated factory per test to avoid contaminating the shared seeded accounts.
    // ============================================================

    [TestMethod]
    public async Task Should_Return401OnTaskEndpoints_When_UserIsDeactivated()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();

        // Register a fresh user in the isolated factory
        var email = $"deactivated-task-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Deactivated Task User" });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        var userClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Verify access works before deactivation
        var beforeResponse = await userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.OK, beforeResponse.StatusCode, "User should access tasks before deactivation");

        // Deactivate via sysadmin
        var userId = await FindUserIdByEmailAsync(sysAdminClient, email);
        var deactivateResponse = await sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, deactivateResponse.StatusCode, "Deactivation must succeed");

        // All task endpoints must now return 401 (ActiveUserMiddleware)
        var listResponse = await userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, listResponse.StatusCode,
            "Deactivated user must get 401 on GET /api/tasks — ActiveUserMiddleware must block them");

        var createResponse = await userClient.PostAsJsonAsync("/api/tasks", new { Title = "Should be blocked" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, createResponse.StatusCode,
            "Deactivated user must get 401 on POST /api/tasks");
    }

    [TestMethod]
    public async Task Should_Return401OnBoardEndpoints_When_UserIsDeactivated()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();

        var email = $"deactivated-board-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Deactivated Board User" });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        var userClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var userId = await FindUserIdByEmailAsync(sysAdminClient, email);
        await sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);

        var boardResponse = await userClient.GetAsync("/api/boards/default");
        Assert.AreEqual(HttpStatusCode.Unauthorized, boardResponse.StatusCode,
            "Deactivated user must get 401 on GET /api/boards/default — ActiveUserMiddleware must block them");
    }

    [TestMethod]
    public async Task Should_Return401OnNotificationEndpoints_When_UserIsDeactivated()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();

        var email = $"deactivated-notif-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Deactivated Notif User" });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        var userClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var userId = await FindUserIdByEmailAsync(sysAdminClient, email);
        await sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);

        var notifResponse = await userClient.GetAsync("/api/notifications");
        Assert.AreEqual(HttpStatusCode.Unauthorized, notifResponse.StatusCode,
            "Deactivated user must get 401 on GET /api/notifications");

        var unreadCountResponse = await userClient.GetAsync("/api/notifications/unread-count");
        Assert.AreEqual(HttpStatusCode.Unauthorized, unreadCountResponse.StatusCode,
            "Deactivated user must get 401 on GET /api/notifications/unread-count");
    }

    [TestMethod]
    public async Task Should_Return401OnRefreshToken_When_UserIsDeactivated()
    {
        // After deactivation, the refresh token must not produce new access tokens.
        // AuthService.RefreshTokenAsync checks for deactivation before issuing new tokens.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();

        var email = $"deactivated-refresh-{Guid.NewGuid():N}@lemondo.dev";
        var password = "TestPass123!";

        // Register (no cookie handling — we'll manually extract Set-Cookie)
        var cookieClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await cookieClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = password, DisplayName = "Deactivated Refresh Test" });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        // Login to get a fresh refresh token in the Set-Cookie header
        var loginResponse = await cookieClient.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = password });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode, "Login should succeed");
        var refreshCookie = ExtractRefreshTokenCookie(loginResponse);
        Assert.IsFalse(string.IsNullOrEmpty(refreshCookie), "Login must set a refresh_token cookie");

        // Deactivate the user
        var userId = auth!.User.Id;
        var deactivateResponse = await sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, deactivateResponse.StatusCode, "Deactivation must succeed");

        // Attempt to refresh token after deactivation — must be rejected
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);
        var refreshResponse = await cookieClient.SendAsync(refreshRequest);

        Assert.AreEqual(HttpStatusCode.Unauthorized, refreshResponse.StatusCode,
            "Deactivated user must not be able to refresh tokens. " +
            "AuthService.RefreshTokenAsync checks for deactivation via IsDeactivated(user).");
    }

    [TestMethod]
    public async Task Should_Return401OnAdminEndpoints_When_AdminIsDeactivated()
    {
        // An admin that has been deactivated must not access admin endpoints.
        using var isolatedFactory = CreateIsolatedFactory();
        var sysAdminClient = await isolatedFactory.CreateSystemAdminClientAsync();

        // Register a user and elevate to Admin
        var email = $"deactivated-admin-{Guid.NewGuid():N}@lemondo.dev";
        var registerClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Temp Admin" });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        var tempAdminId = auth!.User.Id;

        // Elevate to Admin
        await sysAdminClient.PostAsJsonAsync($"/api/admin/users/{tempAdminId}/roles",
            new { RoleName = "Admin" });

        // Login as the temp admin to get a fresh token with Admin role
        var loginResponse = await registerClient.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "TestPass123!" });
        var tempAdminAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        var tempAdminClient = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        tempAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tempAdminAuth!.AccessToken);

        // Verify temp admin can access admin endpoints before deactivation
        var beforeResponse = await tempAdminClient.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.OK, beforeResponse.StatusCode,
            "Temp admin should access admin endpoints before deactivation");

        // Deactivate the temp admin
        var deactivateResponse = await sysAdminClient.PostAsync($"/api/admin/users/{tempAdminId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, deactivateResponse.StatusCode, "Deactivation must succeed");

        // Admin endpoints must now return 401 (ActiveUserMiddleware blocks deactivated users)
        var afterResponse = await tempAdminClient.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, afterResponse.StatusCode,
            "Deactivated admin must get 401 on admin endpoints — " +
            "ActiveUserMiddleware runs after authorization and blocks deactivated users");
    }

    // ============================================================
    // CATEGORY 7: Error Response 401/403/404 Consistency
    // ============================================================

    [TestMethod]
    public async Task Should_Return403_When_RegularUserAccessesAdminEndpoints()
    {
        var userClient = await _factory.CreateAuthenticatedClientAsync();

        var endpoints = new[]
        {
            ("GET", "/api/admin/users"),
            ("GET", "/api/admin/audit"),
        };

        foreach (var (method, path) in endpoints)
        {
            var response = method == "GET"
                ? await userClient.GetAsync(path)
                : await userClient.PostAsync(path, null);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
                $"Regular user must get 403 (not 404) on {method} {path}. Got: {(int)response.StatusCode}");
        }
    }

    [TestMethod]
    public async Task Should_Return403_When_AdminAccessesSystemAdminOnlyEndpoints()
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        var randomId = Guid.NewGuid();

        var endpoints = new[]
        {
            (HttpMethod.Post, $"/api/admin/users/{randomId}/deactivate"),
            (HttpMethod.Post, $"/api/admin/users/{randomId}/reactivate"),
            (HttpMethod.Delete, $"/api/admin/users/{randomId}/roles/Admin"),
        };

        foreach (var (method, path) in endpoints)
        {
            HttpResponseMessage response;
            if (method == HttpMethod.Post)
                response = await adminClient.PostAsync(path, null);
            else
                response = await adminClient.DeleteAsync(path);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
                $"Admin (non-SystemAdmin) must get 403 on {method.Method} {path}. Got: {(int)response.StatusCode}");
        }
    }

    [TestMethod]
    public async Task Should_Return401_When_UnauthenticatedAccessToAdminEndpoints()
    {
        var anonClient = CreateFreshClient();

        var endpoints = new[]
        {
            ("GET", "/api/admin/users"),
            ("GET", "/api/admin/audit"),
            ("POST", $"/api/admin/users/{Guid.NewGuid()}/deactivate"),
            ("POST", $"/api/admin/users/{Guid.NewGuid()}/reactivate"),
        };

        foreach (var (method, path) in endpoints)
        {
            HttpResponseMessage response;
            if (method == "GET")
                response = await anonClient.GetAsync(path);
            else
                response = await anonClient.PostAsync(path, null);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
                $"Unauthenticated request to {method} {path} must return 401. Got: {(int)response.StatusCode}");
        }
    }

    // ============================================================
    // CATEGORY 8: Audit Log Content Isolation
    // ============================================================

    [TestMethod]
    public async Task Should_ReturnZeroResults_When_FilteringAuditByNonExistentActorId()
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        var randomActorId = Guid.NewGuid();

        var response = await adminClient.GetAsync($"/api/admin/audit?actorId={randomActorId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(JsonOpts);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount,
            "Filtering audit log by a non-existent actorId must return 0 entries");
    }

    [TestMethod]
    public async Task Should_Return400_When_AuditLogActorIdIsInvalidGuid()
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        var response = await adminClient.GetAsync("/api/admin/audit?actorId=not-a-guid");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Invalid actorId (non-GUID) must not crash the server");
        // ASP.NET binding of Nullable<Guid> from "not-a-guid" fails → 400
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Invalid actorId must result in 200 (param ignored) or 400, got {(int)response.StatusCode}");
    }

    // ============================================================
    // UTILITIES
    // ============================================================

    /// <summary>
    /// Builds a signed JWT with the test secret key. The sub claim is required.
    /// Optional extraClaims is a JSON fragment appended inside the payload (e.g., ",\"nbf\":123").
    /// </summary>
    private static string BuildSignedToken(
        string sub,
        long? exp = null,
        string extraClaims = "")
    {
        const string secretKey = "test-secret-key-at-least-32-characters-long!!";
        var expValue = exp ?? DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payloadJson = $"{{\"sub\":\"{sub}\",\"jti\":\"{Guid.NewGuid()}\",\"iss\":\"LemonDo\",\"aud\":\"LemonDo\",\"exp\":{expValue}{extraClaims}}}";
        return SignPayload(payloadJson, secretKey);
    }

    /// <summary>
    /// Signs a raw JSON payload string with HMAC-SHA256 and returns the full JWT string.
    /// </summary>
    private static string SignPayload(string payloadJson, string secretKey)
    {
        const string headerJson = """{"alg":"HS256","typ":"JWT"}""";
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var sigInput = $"{header}.{payload}";

        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var sig = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(sigInput)));

        return $"{header}.{payload}.{sig}";
    }

    /// <summary>Converts bytes to Base64Url encoding (URL-safe, no padding).</summary>
    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    // ──────────────────────────────────────────────
    // Local DTO shims matching server wire format.
    // ──────────────────────────────────────────────

    /// <summary>Board DTO matching the server's wire format for /api/boards/default.</summary>
    private sealed record BoardDto(Guid Id, IReadOnlyList<ColumnDto> Columns);

    /// <summary>Column DTO within a board.</summary>
    private sealed record ColumnDto(Guid Id, string Name, string TargetStatus);
}
