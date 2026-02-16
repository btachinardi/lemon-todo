using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LemonDo.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SplitTaskBoardContexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardTaskTags");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ColumnId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ColumnId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Tasks");

            migrationBuilder.CreateTable(
                name: "TaskCards",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ColumnId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCards", x => new { x.BoardId, x.TaskId });
                    table.ForeignKey(
                        name: "FK_TaskCards_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskTags",
                columns: table => new
                {
                    Value = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTags", x => new { x.TaskId, x.Value });
                    table.ForeignKey(
                        name: "FK_TaskTags_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCards");

            migrationBuilder.DropTable(
                name: "TaskTags");

            migrationBuilder.AddColumn<Guid>(
                name: "ColumnId",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BoardTaskTags",
                columns: table => new
                {
                    BoardTaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardTaskTags", x => new { x.BoardTaskId, x.Value });
                    table.ForeignKey(
                        name: "FK_BoardTaskTags_Tasks_BoardTaskId",
                        column: x => x.BoardTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ColumnId",
                table: "Tasks",
                column: "ColumnId");
        }
    }
}
