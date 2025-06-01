using Lounge.Web.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Text.RegularExpressions;

#nullable disable

namespace Lounge.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerGameRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerGameRegistrations",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Game = table.Column<int>(type: "int", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGameRegistrations", x => new { x.PlayerId, x.Game });
                    table.ForeignKey(
                        name: "FK_PlayerGameRegistrations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameRegistrations_Game",
                table: "PlayerGameRegistrations",
                column: "Game");

            migrationBuilder.Sql("ALTER TABLE PlayerGameRegistrations ENABLE CHANGE_TRACKING", true);

            migrationBuilder.Sql(
                @"INSERT INTO PlayerGameRegistrations (PlayerId, Game, RegisteredOn) 
                  SELECT PlayerId, 0, MIN(AwardedOn) FROM Placements
                  GROUP BY PlayerId", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGameRegistrations");
        }
    }
}
