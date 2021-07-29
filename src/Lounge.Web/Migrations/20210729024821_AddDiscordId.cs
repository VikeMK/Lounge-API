using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddDiscordId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordId",
                table: "Players",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_DiscordId",
                table: "Players",
                column: "DiscordId",
                unique: true,
                filter: "[DiscordId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_DiscordId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "Players");
        }
    }
}
