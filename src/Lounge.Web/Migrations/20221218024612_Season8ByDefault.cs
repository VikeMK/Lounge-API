using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lounge.Web.Migrations
{
    public partial class Season8ByDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 7);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 7,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);
        }
    }
}
