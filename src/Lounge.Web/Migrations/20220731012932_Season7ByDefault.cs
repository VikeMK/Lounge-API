using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class Season7ByDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 6);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);
        }
    }
}
