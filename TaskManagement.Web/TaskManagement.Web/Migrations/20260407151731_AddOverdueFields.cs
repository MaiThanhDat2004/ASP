using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOverdueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtendCount",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalDeadline",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueMarkedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverdueMarkedBy",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverdueReason",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtendCount",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "OriginalDeadline",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "OverdueMarkedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "OverdueMarkedBy",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "OverdueReason",
                table: "Tasks");
        }
    }
}
