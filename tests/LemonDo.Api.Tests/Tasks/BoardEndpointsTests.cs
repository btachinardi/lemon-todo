namespace LemonDo.Api.Tests.Tasks;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;

[TestClass]
public sealed class BoardEndpointsTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task GetDefaultBoard_CreatesDefaultWith3Columns()
    {
        var response = await _client.GetAsync("/api/boards/default");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var board = await response.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        Assert.IsNotNull(board);
        Assert.AreEqual("My Board", board.Name);
        Assert.HasCount(3, board.Columns);
        Assert.AreEqual("To Do", board.Columns[0].Name);
        Assert.AreEqual("In Progress", board.Columns[1].Name);
        Assert.AreEqual("Done", board.Columns[2].Name);
    }

    [TestMethod]
    public async Task GetBoardById_WithExistingBoard_Returns200()
    {
        var defaultResponse = await _client.GetAsync("/api/boards/default");
        var defaultBoard = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        var response = await _client.GetAsync($"/api/boards/{defaultBoard!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var board = await response.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        Assert.IsNotNull(board);
        Assert.AreEqual(defaultBoard.Id, board.Id);
    }

    [TestMethod]
    public async Task GetBoardById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task AddColumn_ToBoard_Returns201()
    {
        var defaultResponse = await _client.GetAsync("/api/boards/default");
        var board = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{board!.Id}/columns",
            new { Name = "Review", TargetStatus = "InProgress" });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var column = await response.Content.ReadFromJsonAsync<ColumnDto>(JsonOpts);
        Assert.IsNotNull(column);
        Assert.AreEqual("Review", column.Name);
    }

    [TestMethod]
    public async Task RenameColumn_Returns200()
    {
        var defaultResponse = await _client.GetAsync("/api/boards/default");
        var board = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);
        var columnId = board!.Columns[0].Id;

        var response = await _client.PutAsJsonAsync(
            $"/api/boards/{board.Id}/columns/{columnId}",
            new { Name = "Backlog" });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var column = await response.Content.ReadFromJsonAsync<ColumnDto>(JsonOpts);
        Assert.IsNotNull(column);
        Assert.AreEqual("Backlog", column.Name);
    }

    [TestMethod]
    public async Task RemoveColumn_Returns200()
    {
        var defaultResponse = await _client.GetAsync("/api/boards/default");
        var board = await defaultResponse.Content.ReadFromJsonAsync<BoardDto>(JsonOpts);

        // Add an extra column (InProgress so it can be removed)
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/boards/{board!.Id}/columns",
            new { Name = "Temporary", TargetStatus = "InProgress" });
        var addedColumn = await addResponse.Content.ReadFromJsonAsync<ColumnDto>(JsonOpts);

        var response = await _client.DeleteAsync(
            $"/api/boards/{board.Id}/columns/{addedColumn!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
