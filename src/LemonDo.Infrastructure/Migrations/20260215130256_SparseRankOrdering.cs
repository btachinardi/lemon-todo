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
            migrationBuilder.DropColumn(
                name: "Position",
                table: "TaskCards");

            migrationBuilder.AddColumn<decimal>(
                name: "Rank",
                table: "TaskCards",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NextRank",
                table: "Columns",
                type: "TEXT",
                nullable: false,
                defaultValue: 1000m);
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
