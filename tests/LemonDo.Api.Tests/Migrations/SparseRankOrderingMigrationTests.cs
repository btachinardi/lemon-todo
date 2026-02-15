namespace LemonDo.Api.Tests.Migrations;

using LemonDo.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Verifies that the SparseRankOrdering migration correctly converts
/// pre-existing TaskCard Position (int) values into unique Rank (decimal)
/// values that preserve the original ordering.
/// </summary>
[TestClass]
public sealed class SparseRankOrderingMigrationTests
{
    private SqliteConnection _connection = null!;
    private LemonDoDbContext _dbContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LemonDoDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new LemonDoDbContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Applies migrations up to (and including) SplitTaskBoardContexts,
    /// seeds test data with the old Position-based schema, then applies
    /// SparseRankOrdering and returns the connection for assertions.
    /// </summary>
    private void ApplyMigrationsWithTestData(
        Guid boardId, Guid columnId, (Guid taskId, int position)[] cards)
    {
        var migrator = _dbContext.GetInfrastructure().GetRequiredService<IMigrator>();

        // Step 1: Apply up to SplitTaskBoardContexts (old schema with Position)
        migrator.Migrate("SplitTaskBoardContexts");

        // Step 2: Seed test data using raw SQL (EF model doesn't match old schema)
        var now = DateTimeOffset.UtcNow.ToString("o");

        ExecuteSql($"""
            INSERT INTO Boards (Id, OwnerId, Name, CreatedAt, UpdatedAt)
            VALUES ('{boardId}', '{Guid.NewGuid()}', 'Test Board', '{now}', '{now}');
        """);

        ExecuteSql($"""
            INSERT INTO Columns (Id, Name, TargetStatus, Position, MaxTasks, BoardId)
            VALUES ('{columnId}', 'To Do', 'Todo', 0, NULL, '{boardId}');
        """);

        foreach (var (taskId, position) in cards)
        {
            ExecuteSql($"""
                INSERT INTO Tasks (Id, OwnerId, Title, Priority, Status, IsArchived, IsDeleted, CreatedAt, UpdatedAt)
                VALUES ('{taskId}', '{Guid.NewGuid()}', 'Task {position}', 'None', 'Todo', 0, 0, '{now}', '{now}');
            """);

            ExecuteSql($"""
                INSERT INTO TaskCards (BoardId, TaskId, ColumnId, Position)
                VALUES ('{boardId}', '{taskId}', '{columnId}', {position});
            """);
        }

        // Step 3: Apply the migration under test
        migrator.Migrate("SparseRankOrdering");
    }

    private void ExecuteSql(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private List<(Guid taskId, decimal rank)> QueryCardRanks()
    {
        var results = new List<(Guid, decimal)>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT TaskId, Rank FROM TaskCards ORDER BY Rank;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var taskId = Guid.Parse(reader.GetString(0));
            var rank = decimal.Parse(reader.GetString(1));
            results.Add((taskId, rank));
        }
        return results;
    }

    private decimal QueryColumnNextRank(Guid columnId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT NextRank FROM Columns WHERE Id = '{columnId}';";
        var result = cmd.ExecuteScalar();
        return decimal.Parse(result!.ToString()!);
    }

    [TestMethod]
    public void Should_AssignUniqueRanks_When_MultipleCardsExist()
    {
        var boardId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var task0 = Guid.NewGuid();
        var task1 = Guid.NewGuid();
        var task2 = Guid.NewGuid();

        ApplyMigrationsWithTestData(boardId, columnId, [
            (task0, 0),
            (task1, 1),
            (task2, 2),
        ]);

        var ranks = QueryCardRanks();

        // All ranks must be unique
        var uniqueRanks = ranks.Select(r => r.rank).Distinct().ToList();
        Assert.HasCount(3, uniqueRanks);
    }

    [TestMethod]
    public void Should_PreservePositionOrdering_When_MigratingToRanks()
    {
        var boardId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var task0 = Guid.NewGuid();
        var task1 = Guid.NewGuid();
        var task2 = Guid.NewGuid();

        ApplyMigrationsWithTestData(boardId, columnId, [
            (task0, 0),
            (task1, 1),
            (task2, 2),
        ]);

        var ranks = QueryCardRanks();

        // Ranks must be in ascending order matching original Position order
        var rankOfTask0 = ranks.First(r => r.taskId == task0).rank;
        var rankOfTask1 = ranks.First(r => r.taskId == task1).rank;
        var rankOfTask2 = ranks.First(r => r.taskId == task2).rank;

        Assert.IsTrue(rankOfTask0 < rankOfTask1,
            $"Task at Position 0 (rank {rankOfTask0}) should be before Task at Position 1 (rank {rankOfTask1})");
        Assert.IsTrue(rankOfTask1 < rankOfTask2,
            $"Task at Position 1 (rank {rankOfTask1}) should be before Task at Position 2 (rank {rankOfTask2})");
    }

    [TestMethod]
    public void Should_SetColumnNextRankAboveHighestCardRank()
    {
        var boardId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var task0 = Guid.NewGuid();
        var task1 = Guid.NewGuid();
        var task2 = Guid.NewGuid();

        ApplyMigrationsWithTestData(boardId, columnId, [
            (task0, 0),
            (task1, 1),
            (task2, 2),
        ]);

        var ranks = QueryCardRanks();
        var maxRank = ranks.Max(r => r.rank);
        var nextRank = QueryColumnNextRank(columnId);

        Assert.IsTrue(nextRank > maxRank,
            $"Column NextRank ({nextRank}) must be greater than highest card rank ({maxRank})");
    }

    [TestMethod]
    public void Should_HandleEmptyColumn_When_NoCardsExist()
    {
        var boardId = Guid.NewGuid();
        var columnId = Guid.NewGuid();

        ApplyMigrationsWithTestData(boardId, columnId, []);

        var ranks = QueryCardRanks();
        Assert.IsEmpty(ranks);

        // NextRank should be a positive value (the migration default)
        var nextRank = QueryColumnNextRank(columnId);
        Assert.IsTrue(nextRank > 0, $"Column NextRank ({nextRank}) should be positive");
    }

    [TestMethod]
    public void Should_HandleSingleCard_When_OnlyOneExists()
    {
        var boardId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var task0 = Guid.NewGuid();

        ApplyMigrationsWithTestData(boardId, columnId, [
            (task0, 0),
        ]);

        var ranks = QueryCardRanks();
        Assert.HasCount(1, ranks);
        Assert.IsTrue(ranks[0].rank > 0, "Single card rank should be greater than 0");

        var nextRank = QueryColumnNextRank(columnId);
        Assert.IsTrue(nextRank > ranks[0].rank,
            $"NextRank ({nextRank}) must be above single card rank ({ranks[0].rank})");
    }
}
