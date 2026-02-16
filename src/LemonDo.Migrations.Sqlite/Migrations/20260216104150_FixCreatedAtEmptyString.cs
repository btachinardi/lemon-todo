using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class FixCreatedAtEmptyString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix: AddUserAdminColumns migration used defaultValue: "" for CreatedAt,
            // which causes FormatException when EF Core reads the row (empty string
            // is not a valid DateTimeOffset). Backfill with a sensible default.
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET CreatedAt = '2026-02-15T00:00:00.0000000+00:00' WHERE CreatedAt = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
