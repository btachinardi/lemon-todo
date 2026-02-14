namespace LemonDo.Api.Tests.Tasks;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;

[TestClass]
public sealed class TaskEndpointsTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        // Ensure default board exists (auto-created on first access)
        await _client.GetAsync("/api/boards/default");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task CreateTask_WithValidData_Returns201()
    {
        var request = new { Title = "Test task", Description = "A description", Priority = "Medium" };
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        Assert.IsNotNull(dto);
        Assert.AreEqual("Test task", dto.Title);
        Assert.AreEqual("A description", dto.Description);
        Assert.AreEqual("Medium", dto.Priority);
        Assert.AreEqual("Todo", dto.Status);
        Assert.AreNotEqual(Guid.Empty, dto.Id);
    }

    [TestMethod]
    public async Task CreateTask_WithEmptyTitle_Returns400()
    {
        var request = new { Title = "", Priority = "None" };
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateTask_WithTags_PersistsTags()
    {
        var request = new { Title = "Tagged task", Tags = new[] { "urgent", "frontend" } };
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        Assert.IsNotNull(dto);
        Assert.HasCount(2, dto.Tags);
        Assert.IsTrue(dto.Tags.Contains("urgent"));
        Assert.IsTrue(dto.Tags.Contains("frontend"));
    }

    [TestMethod]
    public async Task GetTaskById_WithExistingTask_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Get by ID test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _client.GetAsync($"/api/tasks/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        Assert.IsNotNull(dto);
        Assert.AreEqual("Get by ID test", dto.Title);
    }

    [TestMethod]
    public async Task GetTaskById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTask_WithValidData_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Original title" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tasks/{created!.Id}",
            new { Title = "Updated title", Priority = "High" });

        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        var dto = await updateResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);
        Assert.IsNotNull(dto);
        Assert.AreEqual("Updated title", dto.Title);
        Assert.AreEqual("High", dto.Priority);
    }

    [TestMethod]
    public async Task CompleteTask_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Task to complete" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _client.PostAsync($"/api/tasks/{created!.Id}/complete", null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CompleteTask_ThenUncomplete_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Complete then uncomplete" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        await _client.PostAsync($"/api/tasks/{created!.Id}/complete", null);
        var response = await _client.PostAsync($"/api/tasks/{created.Id}/uncomplete", null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteTask_SoftDeletes_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Task to delete" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _client.DeleteAsync($"/api/tasks/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task MoveTask_ToColumn_Returns200()
    {
        // Get default board to get a column ID
        var boardResponse = await _client.GetAsync("/api/boards/default");
        var board = await boardResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Task to move" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var columnId = board!.Columns[0].Id;
        var response = await _client.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/move",
            new { ColumnId = columnId, Position = 0 });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task AddTag_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Tag test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _client.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/tags",
            new { Tag = "important" });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task RemoveTag_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tasks",
            new { Title = "Remove tag test", Tags = new[] { "toremove" } });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response = await _client.DeleteAsync($"/api/tasks/{created!.Id}/tags/toremove");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ListTasks_ReturnsPagedResult()
    {
        // Create a task to ensure list is not empty
        await _client.PostAsJsonAsync("/api/tasks", new { Title = "List test task" });

        var response = await _client.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TaskDto>>(JsonOpts);
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.Items);
    }

    [TestMethod]
    public async Task BulkComplete_CompletesMultipleTasks()
    {
        var response1 = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Bulk 1" });
        var task1 = await response1.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var response2 = await _client.PostAsJsonAsync("/api/tasks", new { Title = "Bulk 2" });
        var task2 = await response2.Content.ReadFromJsonAsync<TaskDto>(JsonOpts);

        var bulkResponse = await _client.PostAsJsonAsync("/api/tasks/bulk/complete",
            new { TaskIds = new[] { task1!.Id, task2!.Id } });

        Assert.AreEqual(HttpStatusCode.OK, bulkResponse.StatusCode);
    }
}
