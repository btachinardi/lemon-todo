namespace LemonDo.Api.Tests.Authorization;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;

/// <summary>
/// Tests that authenticated users cannot access or modify resources belonging to other users.
/// These are IDOR (Insecure Direct Object Reference) regression tests.
/// Every test registers two fresh users, creates a resource as User A,
/// then verifies User B receives 404 (not 200) when targeting that resource.
/// </summary>
[TestClass]
public sealed class ResourceOwnershipTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static void ClassInit(TestContext _) =>
        _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    // ──────────────────────────────────────────────
    //  Task read endpoints
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task GetTaskById_WithOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-get-task-a");
        using var clientB = await RegisterFreshUserAsync("idor-get-task-b");

        // User A creates a task
        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "User A private task" });
        createResponse.EnsureSuccessStatusCode();
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to read it
        var response = await clientB.GetAsync($"/api/tasks/{task!.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to read User A's task");
    }

    [TestMethod]
    public async Task ListTasks_DoesNotIncludeOtherUsersTasks()
    {
        using var clientA = await RegisterFreshUserAsync("idor-list-task-a");
        using var clientB = await RegisterFreshUserAsync("idor-list-task-b");

        // User A creates a task with a unique title
        var uniqueTitle = $"OnlyForA-{Guid.NewGuid():N}";
        await clientA.PostAsJsonAsync("/api/tasks", new { Title = uniqueTitle });

        // User B lists their tasks
        var response = await clientB.GetAsync("/api/tasks");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TaskDto>>(JsonOpts);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Items.Any(t => t.Title == uniqueTitle),
            "User B's task list should NOT contain User A's task");
    }

    // ──────────────────────────────────────────────
    //  Task mutation endpoints
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateTask_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-update-a");
        using var clientB = await RegisterFreshUserAsync("idor-update-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to update User A's task
        var response = await clientB.PutAsJsonAsync(
            $"/api/tasks/{task!.Id}",
            new { Title = "Hacked by B" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to update User A's task");
    }

    [TestMethod]
    public async Task DeleteTask_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-delete-a");
        using var clientB = await RegisterFreshUserAsync("idor-delete-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's deletable task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to delete User A's task
        var response = await clientB.DeleteAsync($"/api/tasks/{task!.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to delete User A's task");
    }

    [TestMethod]
    public async Task CompleteTask_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-complete-a");
        using var clientB = await RegisterFreshUserAsync("idor-complete-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's completable task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to complete User A's task
        var response = await clientB.PostAsync($"/api/tasks/{task!.Id}/complete", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to complete User A's task");
    }

    [TestMethod]
    public async Task UncompleteTask_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-uncomplete-a");
        using var clientB = await RegisterFreshUserAsync("idor-uncomplete-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's task to uncomplete" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        await clientA.PostAsync($"/api/tasks/{task!.Id}/complete", null);

        // User B tries to uncomplete User A's task
        var response = await clientB.PostAsync($"/api/tasks/{task.Id}/uncomplete", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to uncomplete User A's task");
    }

    [TestMethod]
    public async Task ArchiveTask_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-archive-a");
        using var clientB = await RegisterFreshUserAsync("idor-archive-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's archivable task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to archive User A's task
        var response = await clientB.PostAsync($"/api/tasks/{task!.Id}/archive", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to archive User A's task");
    }

    [TestMethod]
    public async Task AddTag_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-addtag-a");
        using var clientB = await RegisterFreshUserAsync("idor-addtag-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's taggable task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to add a tag to User A's task
        var response = await clientB.PostAsJsonAsync(
            $"/api/tasks/{task!.Id}/tags",
            new { Tag = "hacked" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to add tags to User A's task");
    }

    [TestMethod]
    public async Task RemoveTag_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-rmtag-a");
        using var clientB = await RegisterFreshUserAsync("idor-rmtag-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks",
            new { Title = "A's tagged task", Tags = new[] { "secret" } });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to remove User A's tag
        var response = await clientB.DeleteAsync($"/api/tasks/{task!.Id}/tags/secret");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to remove tags from User A's task");
    }

    [TestMethod]
    public async Task MoveTask_OnOtherUsersTask_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-move-a");
        using var clientB = await RegisterFreshUserAsync("idor-move-b");

        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's movable task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // Get User B's board to use a valid column ID
        var boardResponse = await clientB.GetAsync("/api/boards/default");
        var board = await boardResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        var columnId = board!.Columns[1].Id; // InProgress column

        // User B tries to move User A's task to B's board
        var response = await clientB.PostAsJsonAsync(
            $"/api/tasks/{task!.Id}/move",
            new { ColumnId = columnId, PreviousTaskId = (Guid?)null, NextTaskId = (Guid?)null });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to move User A's task");
    }

    [TestMethod]
    public async Task BulkComplete_OnOtherUsersTasks_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-bulk-a");
        using var clientB = await RegisterFreshUserAsync("idor-bulk-b");

        var r1 = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's bulk task 1" });
        var t1 = await r1.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        var r2 = await clientA.PostAsJsonAsync("/api/tasks", new { Title = "A's bulk task 2" });
        var t2 = await r2.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        // User B tries to bulk-complete User A's tasks
        var response = await clientB.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = new[] { t1!.Id, t2!.Id } });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to bulk-complete User A's tasks");
    }

    // ──────────────────────────────────────────────
    //  Board read endpoints
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task GetBoardById_WithOtherUsersBoard_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-get-board-a");
        using var clientB = await RegisterFreshUserAsync("idor-get-board-b");

        // Get User A's default board ID
        var defaultResponse = await clientA.GetAsync("/api/boards/default");
        var boardA = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        // User B tries to read User A's board by ID
        var response = await clientB.GetAsync($"/api/boards/{boardA!.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to read User A's board");
    }

    // ──────────────────────────────────────────────
    //  Board mutation endpoints
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task AddColumn_OnOtherUsersBoard_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-addcol-a");
        using var clientB = await RegisterFreshUserAsync("idor-addcol-b");

        var defaultResponse = await clientA.GetAsync("/api/boards/default");
        var boardA = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        // User B tries to add a column to User A's board
        var response = await clientB.PostAsJsonAsync(
            $"/api/boards/{boardA!.Id}/columns",
            new { Name = "Injected Column", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to add columns to User A's board");
    }

    [TestMethod]
    public async Task RenameColumn_OnOtherUsersBoard_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-renamecol-a");
        using var clientB = await RegisterFreshUserAsync("idor-renamecol-b");

        var defaultResponse = await clientA.GetAsync("/api/boards/default");
        var boardA = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        var columnId = boardA!.Columns[0].Id;

        // User B tries to rename a column on User A's board
        var response = await clientB.PutAsJsonAsync(
            $"/api/boards/{boardA.Id}/columns/{columnId}",
            new { Name = "Hacked Column" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to rename columns on User A's board");
    }

    [TestMethod]
    public async Task RemoveColumn_OnOtherUsersBoard_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-rmcol-a");
        using var clientB = await RegisterFreshUserAsync("idor-rmcol-b");

        var defaultResponse = await clientA.GetAsync("/api/boards/default");
        var boardA = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        // User A adds a removable column
        var addResponse = await clientA.PostAsJsonAsync(
            $"/api/boards/{boardA!.Id}/columns",
            new { Name = "Temp Column", TargetStatus = "InProgress" });
        var addedColumn = await addResponse.Content.ReadFromJsonAsync<ColumnDto>(JsonOpts);

        // User B tries to remove it
        var response = await clientB.DeleteAsync(
            $"/api/boards/{boardA.Id}/columns/{addedColumn!.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to remove columns from User A's board");
    }

    [TestMethod]
    public async Task ReorderColumn_OnOtherUsersBoard_Returns404()
    {
        using var clientA = await RegisterFreshUserAsync("idor-reordercol-a");
        using var clientB = await RegisterFreshUserAsync("idor-reordercol-b");

        var defaultResponse = await clientA.GetAsync("/api/boards/default");
        var boardA = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        var columnId = boardA!.Columns[0].Id;

        // User B tries to reorder columns on User A's board
        var response = await clientB.PostAsJsonAsync(
            $"/api/boards/{boardA.Id}/columns/reorder",
            new { ColumnId = columnId, NewPosition = 2 });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "User B should NOT be able to reorder columns on User A's board");
    }

    // ──────────────────────────────────────────────
    //  Verify own-resource access still works
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task GetTaskById_WithOwnTask_Returns200()
    {
        using var client = await RegisterFreshUserAsync("idor-own-task");

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new { Title = "My own task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await client.GetAsync($"/api/tasks/{task!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "User should be able to read their own task");
    }

    [TestMethod]
    public async Task GetBoardById_WithOwnBoard_Returns200()
    {
        using var client = await RegisterFreshUserAsync("idor-own-board");

        var defaultResponse = await client.GetAsync("/api/boards/default");
        var board = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        var response = await client.GetAsync($"/api/boards/{board!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "User should be able to read their own board");
    }

    // ──────────────────────────────────────────────
    //  Helper
    // ──────────────────────────────────────────────

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
