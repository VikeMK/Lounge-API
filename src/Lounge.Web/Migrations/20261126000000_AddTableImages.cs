using System;
using Lounge.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lounge.Web.Migrations
{
    /// <summary>
    /// Adds TableImages (which table images exist in the bucket, and their version).
    /// Written by hand rather than with `dotnet ef migrations add`, so there is no .Designer.cs file;
    /// the attributes below are what EF needs to discover it, and ApplicationDbContextModelSnapshot was updated to match.
    /// Change tracking is intentionally NOT enabled on this table (see TableImage).
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261126000000_AddTableImages")]
    public partial class AddTableImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TableImages",
                columns: table => new
                {
                    TableId = table.Column<int>(type: "int", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableImages", x => x.TableId);
                    table.ForeignKey(
                        name: "FK_TableImages_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TableImages");
        }
    }
}
