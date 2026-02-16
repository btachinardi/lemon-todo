namespace LemonDo.Api.Tests.Infrastructure;

using Microsoft.Data.SqlClient;

/// <summary>
/// Assembly-level cleanup that drops all per-instance test databases after the test run.
/// Each CustomWebApplicationFactory creates a unique database (LemonDoTests_{guid}) for
/// parallel test isolation. This cleanup catches any databases that per-factory Dispose missed.
/// </summary>
[TestClass]
public sealed class SqlServerTestCleanup
{
    private static bool UseSqlServer =>
        Environment.GetEnvironmentVariable("TEST_DATABASE_PROVIDER")
            ?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) is true;

    [AssemblyCleanup]
    public static void DropOrphanedTestDatabases()
    {
        if (!UseSqlServer) return;

        try
        {
            var baseConnStr = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION_STRING")
                ?? "Server=localhost,1433;Database=LemonDoTests;User Id=sa;Password=YourStr0ngPassw0rd;TrustServerCertificate=True;";

            var connBuilder = new SqlConnectionStringBuilder(baseConnStr) { InitialCatalog = "master" };

            // Clear all SQL connection pools so no pooled connections block the DROP
            SqlConnection.ClearAllPools();

            using var conn = new SqlConnection(connBuilder.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql = @sql + N'ALTER DATABASE [' + name + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [' + name + N']; '
                FROM sys.databases WHERE name LIKE N'LemonDoTests_%';
                IF @sql <> N'' EXEC sp_executesql @sql;
                """;
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Best effort — container may already be stopped
        }
    }
}
