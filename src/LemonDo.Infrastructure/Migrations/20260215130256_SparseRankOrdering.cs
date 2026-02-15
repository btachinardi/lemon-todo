using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SparseRankOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Rank column BEFORE dropping Position so we can read Position values
            migrationBuilder.AddColumn<decimal>(
                name: "Rank",
                table: "TaskCards",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            // 2. Data migration: convert dense Position (0,1,2,...) to sparse Rank (1000,2000,3000,...)
            migrationBuilder.Sql(
                "UPDATE TaskCards SET Rank = CAST((Position + 1) * 1000 AS TEXT)");

            // 3. Now safe to drop Position
            migrationBuilder.DropColumn(
                name: "Position",
                table: "TaskCards");

            // 4. Add NextRank counter to columns
            migrationBuilder.AddColumn<decimal>(
                name: "NextRank",
                table: "Columns",
                type: "TEXT",
                nullable: false,
                defaultValue: 1000m);

            // 5. Data migration: set NextRank to (highest card rank + 1000) per column,
            //    or keep the default 1000 if the column has no cards
            migrationBuilder.Sql("""
                UPDATE Columns SET NextRank = CAST(
                    COALESCE(
                        (SELECT MAX(CAST(Rank AS REAL)) + 1000 FROM TaskCards WHERE TaskCards.ColumnId = Columns.Id),
                        1000
                    ) AS TEXT)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rank",
                table: "TaskCards");

            migrationBuilder.DropColumn(
                name: "NextRank",
                table: "Columns");

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "TaskCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
