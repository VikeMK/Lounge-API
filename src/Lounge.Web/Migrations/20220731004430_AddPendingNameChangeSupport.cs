using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddPendingNameChangeSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Season",
                table: "NameChanges");

            migrationBuilder.AddColumn<string>(
                name: "NameChangeRequestMessageId",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NameChangeRequestedOn",
                table: "Players",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingName",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_NameChangeRequestedOn",
                table: "Players",
                column: "NameChangeRequestedOn");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_NameChangeRequestedOn",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "NameChangeRequestMessageId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "NameChangeRequestedOn",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PendingName",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "Season",
                table: "NameChanges",
                type: "int",
                nullable: false,
                defaultValue: 6);
        }
    }
}
