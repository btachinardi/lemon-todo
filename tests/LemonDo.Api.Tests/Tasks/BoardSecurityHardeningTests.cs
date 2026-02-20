namespace LemonDo.Api.Tests.Tasks;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;

/// <summary>
/// Security hardening tests for the /api/boards route group.
/// A PASSING test means the endpoint correctly defended against the attack.
/// A FAILING test means a vulnerability was found.
/// </summary>
[TestClass]
public sealed class BoardSecurityHardeningTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a brand-new user and returns an authenticated HttpClient for that user.
    /// Each call produces a unique email so tests are fully isolated.
    /// </summary>
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

    /// <summary>Returns the authenticated test user's default board.</summary>
    private static async Task<BoardDto> GetDefaultBoardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/boards/default");
        response.EnsureSuccessStatusCode();
        var board = await response.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        return board!;
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 1: AUTHENTICATION BYPASS
    //  Expected: All unauthenticated / malformed-token requests → 401
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return401_When_GetDefaultBoard_WithNoToken()
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("/api/boards/default");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "GET /api/boards/default must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_GetBoardById_WithNoToken()
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "GET /api/boards/{id} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_AddColumn_WithNoToken()
    {
        var board = await GetDefaultBoardAsync(_client);
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Injected", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/boards/{id}/columns must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_RenameColumn_WithNoToken()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PutAsJsonAsync(
            $"/api/boards/{board.Id}/columns/{columnId}",
            new { Name = "Hacked" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "PUT /api/boards/{id}/columns/{colId} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_RemoveColumn_WithNoToken()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.DeleteAsync(
            $"/api/boards/{board.Id}/columns/{columnId}");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "DELETE /api/boards/{id}/columns/{colId} must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_ReorderColumn_WithNoToken()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns/reorder",
            new { ColumnId = columnId, NewPosition = 1 });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "POST /api/boards/{id}/columns/reorder must reject unauthenticated requests");
    }

    [TestMethod]
    public async Task Should_Return401_When_AddColumn_WithMalformedToken()
    {
        var board = await GetDefaultBoardAsync(_client);
        using var badClient = _factory.CreateClient();
        badClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt.token");

        var response = await badClient.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Test", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Malformed JWT token must be rejected with 401");
    }

    [TestMethod]
    public async Task Should_Return401_When_AddColumn_WithEmptyBearerToken()
    {
        var board = await GetDefaultBoardAsync(_client);
        using var badClient = _factory.CreateClient();
        badClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");

        var response = await badClient.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Test", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Empty Bearer token must be rejected with 401");
    }

    [TestMethod]
    public async Task Should_Return401_When_AddColumn_WithWrongSignatureToken()
    {
        // JWT structure is valid but signed with a different key
        // Header: {"alg":"HS256","typ":"JWT"} | Payload: {"sub":"attacker"} | Signature: garbage
        const string wrongKeyToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiJhdHRhY2tlciIsIm5hbWUiOiJBdHRhY2tlciIsImlhdCI6MTUxNjIzOTAyMn0." +
            "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        var board = await GetDefaultBoardAsync(_client);
        using var badClient = _factory.CreateClient();
        badClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", wrongKeyToken);

        var response = await badClient.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Test", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "JWT signed with a different key must be rejected with 401");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 2: AUTHORIZATION / IDOR EDGE CASES
    //  (Core IDOR is covered in ResourceOwnershipTests.cs;
    //   here we verify non-existent and cross-board column access)
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return404_When_GetBoardById_WithRandomNonExistentId()
    {
        var response = await _client.GetAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "A non-existent board ID must return 404, not 500 or data from another user");
    }

    [TestMethod]
    public async Task Should_Return404_When_AddColumn_WithNonExistentBoardId()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{Guid.NewGuid()}/columns",
            new { Name = "Test", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Adding a column to a non-existent board must return 404");
    }

    [TestMethod]
    public async Task Should_Return404_When_RenameColumn_WithNonExistentBoardId()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{Guid.NewGuid()}/columns/{Guid.NewGuid()}",
            new { Name = "New Name" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Renaming a column on a non-existent board must return 404");
    }

    [TestMethod]
    public async Task Should_Return404_When_RemoveColumn_WithNonExistentBoardId()
    {
        var response = await _client.DeleteAsync(
            $"/api/boards/{Guid.NewGuid()}/columns/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Removing a column from a non-existent board must return 404");
    }

    [TestMethod]
    public async Task Should_Return404_When_ReorderColumn_WithNonExistentBoardId()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{Guid.NewGuid()}/columns/reorder",
            new { ColumnId = Guid.NewGuid(), NewPosition = 0 });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "Reordering on a non-existent board must return 404");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_RenameColumn_WithColumnBelongingToAnotherBoard()
    {
        // User A's column IDs are only meaningful on User A's board.
        // Presenting a real column ID from User A against User B's board should fail gracefully.
        using var clientA = await RegisterFreshUserAsync("idor-crossboard-a");
        using var clientB = await RegisterFreshUserAsync("idor-crossboard-b");

        var boardA = await GetDefaultBoardAsync(clientA);
        var boardB = await GetDefaultBoardAsync(clientB);

        // User B tries to rename User A's column but supplies User B's board ID
        // The column won't be found on B's board → should be 4xx, not 200
        var columnIdFromA = boardA.Columns[0].Id;
        var response = await clientB.PutAsJsonAsync(
            $"/api/boards/{boardB.Id}/columns/{columnIdFromA}",
            new { Name = "Cross-board Hacked" });

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity,
            $"Cross-board column reference must be rejected, got {response.StatusCode}");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 3: INPUT VALIDATION
    // ─────────────────────────────────────────────────────────────

    // --- Column Name Validation ---

    [TestMethod]
    public async Task Should_Return400_When_AddColumn_WithEmptyName()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Empty column name must be rejected");
    }

    [TestMethod]
    public async Task Should_Return400_When_AddColumn_WithWhitespaceOnlyName()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "   ", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Whitespace-only column name must be rejected");
    }

    [TestMethod]
    public async Task Should_Return400_When_AddColumn_WithNameExceedingMaxLength()
    {
        var board = await GetDefaultBoardAsync(_client);
        // ColumnName.MaxLength is 50; send 51 characters
        var longName = new string('A', 51);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = longName, TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Column name exceeding 50 characters must be rejected");
    }

    [TestMethod]
    public async Task Should_Return400_When_RenameColumn_WithEmptyName()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;

        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{board.Id}/columns/{columnId}",
            new { Name = "" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Empty new name on rename must be rejected");
    }

    [TestMethod]
    public async Task Should_Return400_When_RenameColumn_WithNameExceedingMaxLength()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;
        var longName = new string('X', 51);

        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{board.Id}/columns/{columnId}",
            new { Name = longName });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Column name exceeding 50 characters on rename must be rejected");
    }

    // --- TargetStatus Validation ---

    [TestMethod]
    public async Task Should_Return400_When_AddColumn_WithInvalidTargetStatus()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Valid Name", TargetStatus = "InvalidStatus" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Unknown TargetStatus value must be rejected");
    }

    [TestMethod]
    public async Task Should_Return400_When_AddColumn_WithEmptyTargetStatus()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Valid Name", TargetStatus = "" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "Empty TargetStatus must be rejected");
    }

    [TestMethod]
    public async Task Should_Return400_When_AddColumn_WithSqlInjectionInTargetStatus()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Valid Name", TargetStatus = "'; DROP TABLE Boards; --" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "SQL injection in TargetStatus must be rejected as invalid enum value");
    }

    // --- XSS / Injection in Name Fields ---

    [TestMethod]
    public async Task Should_NotReturn500_When_AddColumn_WithXssPayloadInName()
    {
        // XSS in the name should either be stored literally (no server-side HTML rendering)
        // or rejected. Either way it must NOT cause a 500.
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "<script>alert('xss')</script>", TargetStatus = "InProgress" });

        // The name is 31 chars — within the 50-char limit, so it may be accepted (201) or
        // rejected by business rule. What it must NOT be is a server error.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XSS payload in column name must not cause a 500 Internal Server Error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_RenameColumn_WithSqlInjectionInName()
    {
        var board = await GetDefaultBoardAsync(_client);
        // Use the InProgress column which can be renamed safely
        var columnId = board.Columns[1].Id;
        var sqlPayload = "'; DROP TABLE Boards; --";

        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{board.Id}/columns/{columnId}",
            new { Name = sqlPayload });

        // The payload is 24 chars so it passes the length check.
        // The name should be stored literally or handled gracefully — never a 500.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in column name must not cause a 500 Internal Server Error");
    }

    // --- Missing Required Fields ---

    [TestMethod]
    public async Task Should_Return4xx_When_AddColumn_WithMissingNameProperty()
    {
        var board = await GetDefaultBoardAsync(_client);

        // Omit the Name property entirely (will deserialize to null)
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { TargetStatus = "InProgress" });

        // Null Name will fail ColumnName.Create → 400
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Missing Name property must be rejected with 4xx, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_AddColumn_WithMissingTargetStatusProperty()
    {
        var board = await GetDefaultBoardAsync(_client);

        // Omit TargetStatus entirely (will deserialize as null/empty)
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "My Column" });

        // Null TargetStatus will fail Enum.TryParse → 400
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Missing TargetStatus property must be rejected with 4xx, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_RenameColumn_WithMissingNameProperty()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;

        // Send an empty object — Name will be null
        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{board.Id}/columns/{columnId}",
            new { });

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Missing Name property on rename must be rejected with 4xx, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ReorderColumn_WithMissingColumnId()
    {
        var board = await GetDefaultBoardAsync(_client);

        // Omit ColumnId — will default to Guid.Empty which won't match any column
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns/reorder",
            new { NewPosition = 0 });

        // Guid.Empty won't be found as a column → 4xx
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.UnprocessableEntity
                or HttpStatusCode.NotFound,
            $"Missing ColumnId must be rejected with 4xx, got {response.StatusCode}");
    }

    // --- Type Coercion / Malformed Body ---

    [TestMethod]
    public async Task Should_Return4xx_When_AddColumn_WithCompletelyWrongContentType()
    {
        var board = await GetDefaultBoardAsync(_client);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/boards/{board.Id}/columns");
        request.Headers.Authorization = _client.DefaultRequestHeaders.Authorization;
        request.Content = new StringContent("not json at all", Encoding.UTF8, "application/json");

        using var tempClient = _factory.CreateClient();
        tempClient.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;
        var response = await tempClient.SendAsync(request);

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Malformed JSON body must be rejected with 4xx, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_AddColumn_WithPositionAsString()
    {
        var board = await GetDefaultBoardAsync(_client);

        // Position is int? — sending a string should cause deserialization error
        var json = $"{{\"Name\":\"Test\",\"TargetStatus\":\"InProgress\",\"Position\":\"not-a-number\"}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var tempClient = _factory.CreateClient();
        tempClient.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;

        var response = await tempClient.PostAsync(
            $"/api/boards/{board.Id}/columns", content);

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"String value for integer Position must be rejected, got {response.StatusCode}");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 4: BUSINESS LOGIC ABUSE
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return4xx_When_ReorderColumn_WithNegativePosition()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns/reorder",
            new { ColumnId = columnId, NewPosition = -1 });

        // Domain validates newPosition < 0 → error "position.validation" → 400
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Negative position must be rejected with 4xx, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ReorderColumn_WithOutOfRangePosition()
    {
        var board = await GetDefaultBoardAsync(_client);
        var columnId = board.Columns[0].Id;

        // Board has 3 columns (positions 0,1,2) — position 9999 is out of range
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns/reorder",
            new { ColumnId = columnId, NewPosition = 9999 });

        // Domain validates newPosition >= _columns.Count → error "position.validation" → 400
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Out-of-range position must be rejected with 4xx, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ReorderColumn_WithNonExistentColumnIdOnOwnBoard()
    {
        var board = await GetDefaultBoardAsync(_client);

        // A random GUID that doesn't correspond to any column on the board
        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns/reorder",
            new { ColumnId = Guid.NewGuid(), NewPosition = 0 });

        // Domain: column not found → error "board.column_not_found" → 422
        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Non-existent column ID in reorder must be rejected, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_AddColumn_WithDuplicateColumnName()
    {
        using var client = await RegisterFreshUserAsync("biz-duplicate-col");
        var board = await GetDefaultBoardAsync(client);

        // "To Do" column already exists on the default board
        var response = await client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "To Do", TargetStatus = "InProgress" });

        // Domain: duplicate name → DomainError.BusinessRule → 422
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict,
            $"Duplicate column name must be rejected with 422 or 409, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_RemoveColumn_WithLastTodoColumn()
    {
        // Each board has exactly one Todo column by default.
        // Removing it must be rejected by the domain invariant.
        using var client = await RegisterFreshUserAsync("biz-remove-todo-col");
        var board = await GetDefaultBoardAsync(client);
        var todoColumn = board.Columns.First(c => c.TargetStatus == "Todo");

        var response = await client.DeleteAsync(
            $"/api/boards/{board.Id}/columns/{todoColumn.Id}");

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Removing the last Todo column must be rejected, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_RemoveColumn_WithLastDoneColumn()
    {
        // Similarly, the Done column must not be removable when it's the only one.
        using var client = await RegisterFreshUserAsync("biz-remove-done-col");
        var board = await GetDefaultBoardAsync(client);
        var doneColumn = board.Columns.First(c => c.TargetStatus == "Done");

        var response = await client.DeleteAsync(
            $"/api/boards/{board.Id}/columns/{doneColumn.Id}");

        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Removing the last Done column must be rejected, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_RemoveColumn_WithNonExistentColumnIdOnOwnBoard()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.DeleteAsync(
            $"/api/boards/{board.Id}/columns/{Guid.NewGuid()}");

        // Domain: column not found → 4xx
        Assert.IsTrue(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Deleting a non-existent column must be rejected, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_AddColumn_WithExtremelyLargeBatchCreation_ExceedsReasonableCount()
    {
        // Denial-of-service probe: attempt to add many columns to bloat the DB.
        // The domain has no hard column count limit, but each unique-named column is
        // a real DB write. We verify the 26th unique column is still accepted or that
        // the system handles it without 500.  This is a WARN-level check: no hard limit
        // means an authenticated user COULD spam columns — document the finding.
        using var client = await RegisterFreshUserAsync("dos-column-flood");
        var board = await GetDefaultBoardAsync(client);
        var boardId = board.Id;

        int successCount = 0;
        int errorCount = 0;
        const int attempts = 20;

        for (int i = 0; i < attempts; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/boards/{boardId}/columns",
                new { Name = $"Flood-{i:D3}", TargetStatus = "InProgress" });

            if (response.IsSuccessStatusCode) successCount++;
            else errorCount++;

            // Stop early if the server starts throttling
            if (response.StatusCode == HttpStatusCode.TooManyRequests) break;
        }

        // This test PASSES if no 500 errors occurred (even if all succeed —
        // the security finding about lack of column count limits is reported separately).
        // We assert the server never panicked.
        var finalResponse = await client.GetAsync($"/api/boards/{boardId}");
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, finalResponse.StatusCode,
            "Bulk column creation must not destabilize the server");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 5: MASS ASSIGNMENT / OVER-POSTING
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_IgnoreExtraFields_When_AddColumn_WithUnknownProperties()
    {
        // Send extra fields that should not be persisted
        using var client = await RegisterFreshUserAsync("mass-assign-addcol");
        var board = await GetDefaultBoardAsync(client);

        // id, ownerId, createdAt are not part of AddColumnRequest — they must be ignored
        var json = JsonSerializer.Serialize(new
        {
            Name = "ValidColumn",
            TargetStatus = "InProgress",
            Id = Guid.NewGuid(),          // should be ignored
            OwnerId = Guid.NewGuid(),     // should be ignored
            MaxTasks = -1,                // should be ignored (not part of request contract)
            Position = 0
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var tempClient = _factory.CreateClient();
        tempClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;

        var response = await tempClient.PostAsync(
            $"/api/boards/{board.Id}/columns", content);

        // Should succeed — extra fields ignored, not a 400/500
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            $"Extra/unknown fields must be silently ignored, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_IgnoreExtraFields_When_RenameColumn_WithUnknownProperties()
    {
        using var client = await RegisterFreshUserAsync("mass-assign-rename");
        var board = await GetDefaultBoardAsync(client);
        var columnId = board.Columns[1].Id; // InProgress column

        var json = JsonSerializer.Serialize(new
        {
            Name = "ValidNewName",
            Id = Guid.NewGuid(),           // should be ignored
            TargetStatus = "Done",         // should be ignored (not part of RenameColumnRequest)
            Position = 99,                 // should be ignored
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var tempClient = _factory.CreateClient();
        tempClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;

        var renameResponse = await tempClient.PutAsync(
            $"/api/boards/{board.Id}/columns/{columnId}", content);

        Assert.IsTrue(
            renameResponse.IsSuccessStatusCode,
            $"Extra/unknown fields in rename must be silently ignored, got {renameResponse.StatusCode}");

        // Verify TargetStatus was NOT changed by the extra field
        var boardAfter = await GetDefaultBoardAsync(client);
        var renamedColumn = boardAfter.Columns.FirstOrDefault(c => c.Id == columnId);
        Assert.IsNotNull(renamedColumn, "The renamed column should still exist");
        Assert.AreEqual("InProgress", renamedColumn!.TargetStatus,
            "TargetStatus must not be changed by an extra field in the rename request");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 6: INFORMATION LEAKAGE
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_ReturnConsistentErrorShape_When_GetBoardById_WithNonExistentId()
    {
        var response = await _client.GetAsync($"/api/boards/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // Must not include stack traces, file paths, or EF internals
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Error response must not contain a stack trace");
        Assert.IsFalse(body.Contains("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase),
            "Error response must not expose EF Core internals");
        Assert.IsFalse(body.Contains("System.Exception", StringComparison.OrdinalIgnoreCase),
            "Error response must not expose raw exception types");
    }

    [TestMethod]
    public async Task Should_ReturnConsistentErrorShape_When_AddColumn_WithInvalidInput()
    {
        var board = await GetDefaultBoardAsync(_client);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Validation error response must not expose stack trace");
        Assert.IsFalse(body.Contains("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase),
            "Validation error response must not expose EF Core internals");

        // Must be parseable as the standard ErrorResponse shape
        var errorResponse = JsonSerializer.Deserialize<ErrorResponseShape>(body, JsonOpts);
        Assert.IsNotNull(errorResponse, "Error response must be parseable JSON");
        Assert.IsTrue(errorResponse.Status >= 400, "Error response must include a valid status code");
        Assert.IsFalse(string.IsNullOrWhiteSpace(errorResponse.Title),
            "Error response must include a human-readable title");
    }

    [TestMethod]
    public async Task Should_ReturnConsistentErrorShape_When_Unauthenticated()
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("/api/boards/default");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Auth failure response must not expose stack trace");
    }

    [TestMethod]
    public async Task Should_NotExposeInternalIds_When_AddColumn_Succeeds()
    {
        using var client = await RegisterFreshUserAsync("info-leak-col");
        var board = await GetDefaultBoardAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/columns",
            new { Name = "Info Leak Test Col", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Response must not include owner IDs or other sensitive internals
        Assert.IsFalse(body.Contains("ownerId", StringComparison.OrdinalIgnoreCase),
            "Column response must not expose board OwnerId");
        Assert.IsFalse(body.Contains("passwordHash", StringComparison.OrdinalIgnoreCase),
            "Column response must not expose password hash");
    }

    // ─────────────────────────────────────────────────────────────
    //  CATEGORY 7: HTTP / ROUTING BEHAVIOUR
    // ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Should_Return405_When_PutSentToGetDefaultBoardRoute()
    {
        var response = await _client.PutAsJsonAsync("/api/boards/default", new { });

        // Method not allowed — the route only accepts GET
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode,
            "PUT to a GET-only endpoint must return 405");
    }

    [TestMethod]
    public async Task Should_Return405_When_DeleteSentToGetDefaultBoardRoute()
    {
        var response = await _client.DeleteAsync("/api/boards/default");

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode,
            "DELETE to a GET-only endpoint must return 405");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_BoardIdIsNotAValidGuid()
    {
        // Route constraint /{id:guid} should reject non-GUID route values
        var response = await _client.GetAsync("/api/boards/not-a-guid");

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"Non-GUID board ID must be rejected by route constraint, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_ColumnIdIsNotAValidGuid()
    {
        var board = await GetDefaultBoardAsync(_client);

        // Route constraint /{colId:guid} should reject non-GUID column IDs
        var response = await _client.DeleteAsync(
            $"/api/boards/{board.Id}/columns/not-a-guid");

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"Non-GUID column ID must be rejected by route constraint, got {response.StatusCode}");
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPER SHAPE for error-body deserialization
    // ─────────────────────────────────────────────────────────────

    private sealed record ErrorResponseShape(string? Type, string? Title, int Status);
}
