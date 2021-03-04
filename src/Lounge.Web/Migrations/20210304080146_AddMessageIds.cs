using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddMessageIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TableMessageId",
                table: "Tables",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdateMessageId",
                table: "Tables",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableMessageId",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "UpdateMessageId",
                table: "Tables");
        }
    }
}
