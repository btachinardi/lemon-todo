using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SeparateUserFromIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EncryptedDisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EncryptedEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RedactedEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    RedactedDisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDeactivated = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    EmailHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EncryptedDisplayName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EncryptedEmail = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailHash",
                table: "Users",
                column: "EmailHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.AddColumn<string>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
