﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lounge.Web.Migrations
{
    public partial class Season9ByDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 9,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 9,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 9,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 9,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 9,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 8);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 9);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 9);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 9);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 9);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 9);
        }
    }
}
