using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddRegistryData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistryId",
                table: "Players",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwitchFc",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "RegistryId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SwitchFc",
                table: "Players");
        }
    }
}
