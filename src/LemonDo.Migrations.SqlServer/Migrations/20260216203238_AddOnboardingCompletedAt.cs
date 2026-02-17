using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingCompletedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "Users");
        }
    }
}
