using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOutputOverdueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtendCount",
                table: "TaskOutputRequirements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverdue",
                table: "TaskOutputRequirements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalDeadline",
                table: "TaskOutputRequirements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueMarkedAt",
                table: "TaskOutputRequirements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverdueReason",
                table: "TaskOutputRequirements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtendCount",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "IsOverdue",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "OriginalDeadline",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "OverdueMarkedAt",
                table: "TaskOutputRequirements");

            migrationBuilder.DropColumn(
                name: "OverdueReason",
                table: "TaskOutputRequirements");
        }
    }
}
