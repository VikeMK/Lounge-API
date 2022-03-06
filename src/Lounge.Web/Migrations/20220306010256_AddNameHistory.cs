using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddNameHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NameChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ChangedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NameChanges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NameChanges_ChangedOn",
                table: "NameChanges",
                column: "ChangedOn");

            migrationBuilder.CreateIndex(
                name: "IX_NameChanges_PlayerId",
                table: "NameChanges",
                column: "PlayerId");

            migrationBuilder.Sql(@"
                INSERT INTO NameChanges (Name, NormalizedName, Season, ChangedOn, PlayerId)
                SELECT Name, NormalizedName, 5 as Season, CURRENT_TIMESTAMP as ChangedOn, Id as PlayerId
                FROM Players");

            migrationBuilder.Sql($"ALTER TABLE NameChanges ENABLE CHANGE_TRACKING", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NameChanges");
        }
    }
}
