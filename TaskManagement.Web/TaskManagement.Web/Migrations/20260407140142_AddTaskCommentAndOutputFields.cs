using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCommentAndOutputFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedFileFormat",
                table: "TaskOutputRequirements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "TaskOutputRequirements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryAssigneeId",
                table: "TaskOutputRequirements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportAssigneeIds",
                table: "TaskOutputRequirements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    OutputRequirementId = table.Column<int>(type: "int", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_TaskComments_TaskOutputRequirements_OutputRequirementId",
                        column: x => x.OutputRequirementId,
                        principalTable: "TaskOutputRequirements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskComments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskComments_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskOutputRequirements_PrimaryAssigneeId",
                table: "TaskOutputRequirements",
                column: "PrimaryAssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_OutputRequirementId",
                table: "TaskComments",
                column: "OutputRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_SenderId",
                table: "TaskComments",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId",
                table: "TaskComments",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskOutputRequirements_Users_PrimaryAssigneeId",
                table: "TaskOutputRequirements",
                column: "PrimaryAssigneeId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskOutputRequirements_Users_PrimaryAssigneeId",
                table: "TaskOutputRequirements");

            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropIndex(
                name: "IX_TaskOutputRequirements_PrimaryAssigneeId",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "AllowedFileFormat",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "PrimaryAssigneeId",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "SupportAssigneeIds",
                table: "TaskOutputRequirements");
        }
    }
}
