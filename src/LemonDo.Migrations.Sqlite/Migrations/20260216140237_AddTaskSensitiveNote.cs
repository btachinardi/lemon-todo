using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSensitiveNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedSensitiveNote",
                table: "Tasks",
                type: "TEXT",
                maxLength: 15000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedactedSensitiveNote",
                table: "Tasks",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedSensitiveNote",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RedactedSensitiveNote",
                table: "Tasks");
        }
    }
}
