using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedPiiColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedDisplayName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedEmail",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedDisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EncryptedEmail",
                table: "AspNetUsers");
        }
    }
}
