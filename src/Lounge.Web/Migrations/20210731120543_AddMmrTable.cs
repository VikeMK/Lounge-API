using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddMmrTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Timestamp",
                table: "Tables",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateTable(
                name: "PlayerSeasonData",
                columns: table => new
                {
                    Season = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Mmr = table.Column<int>(type: "int", nullable: false),
                    MaxMmr = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSeasonData", x => new { x.PlayerId, x.Season });
                    table.ForeignKey(
                        name: "FK_PlayerSeasonData_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonData_Season_Mmr",
                table: "PlayerSeasonData",
                columns: new[] { "Season", "Mmr" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerSeasonData");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Tables");
        }
    }
}
