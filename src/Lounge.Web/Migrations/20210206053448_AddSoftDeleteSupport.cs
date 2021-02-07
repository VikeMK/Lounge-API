using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddSoftDeleteSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrevMMR",
                table: "Penalties",
                newName: "PrevMmr");

            migrationBuilder.RenameColumn(
                name: "NewMMR",
                table: "Penalties",
                newName: "NewMmr");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Tables",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Penalties",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Penalties");

            migrationBuilder.RenameColumn(
                name: "PrevMmr",
                table: "Penalties",
                newName: "PrevMMR");

            migrationBuilder.RenameColumn(
                name: "NewMmr",
                table: "Penalties",
                newName: "NewMMR");
        }
    }
}
