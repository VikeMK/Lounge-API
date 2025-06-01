using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lounge.Web.Migrations
{
    /// <inheritdoc />
    public partial class MultiGameSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerSeasonData",
                table: "PlayerSeasonData");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSeasonData_Season_Mmr",
                table: "PlayerSeasonData");

            migrationBuilder.DropIndex(
                name: "IX_Placements_AwardedOn",
                table: "Placements");

            migrationBuilder.DropIndex(
                name: "IX_Penalties_AwardedOn",
                table: "Penalties");

            migrationBuilder.DropIndex(
                name: "IX_Bonuses_AwardedOn",
                table: "Bonuses");

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 13);

            migrationBuilder.AddColumn<int>(
                name: "Game",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 13);

            migrationBuilder.AddColumn<int>(
                name: "Game",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 13);

            migrationBuilder.AddColumn<int>(
                name: "Game",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 13);

            migrationBuilder.AddColumn<int>(
                name: "Game",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 13);

            migrationBuilder.AddColumn<int>(
                name: "Game",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerSeasonData",
                table: "PlayerSeasonData",
                columns: new[] { "PlayerId", "Game", "Season" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonData_Game_Season_Mmr",
                table: "PlayerSeasonData",
                columns: new[] { "Game", "Season", "Mmr" });

            migrationBuilder.CreateIndex(
                name: "IX_Placements_Game_AwardedOn",
                table: "Placements",
                columns: new[] { "Game", "AwardedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_Game_AwardedOn",
                table: "Penalties",
                columns: new[] { "Game", "AwardedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Bonuses_Game_AwardedOn",
                table: "Bonuses",
                columns: new[] { "Game", "AwardedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerSeasonData",
                table: "PlayerSeasonData");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSeasonData_Game_Season_Mmr",
                table: "PlayerSeasonData");

            migrationBuilder.DropIndex(
                name: "IX_Placements_Game_AwardedOn",
                table: "Placements");

            migrationBuilder.DropIndex(
                name: "IX_Penalties_Game_AwardedOn",
                table: "Penalties");

            migrationBuilder.DropIndex(
                name: "IX_Bonuses_Game_AwardedOn",
                table: "Bonuses");

            migrationBuilder.DropColumn(
                name: "Game",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "Game",
                table: "PlayerSeasonData");

            migrationBuilder.DropColumn(
                name: "Game",
                table: "Placements");

            migrationBuilder.DropColumn(
                name: "Game",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "Game",
                table: "Bonuses");

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 13,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "PlayerSeasonData",
                type: "int",
                nullable: false,
                defaultValue: 13,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Placements",
                type: "int",
                nullable: false,
                defaultValue: 13,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 13,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Season",
                table: "Bonuses",
                type: "int",
                nullable: false,
                defaultValue: 13,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerSeasonData",
                table: "PlayerSeasonData",
                columns: new[] { "PlayerId", "Season" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonData_Season_Mmr",
                table: "PlayerSeasonData",
                columns: new[] { "Season", "Mmr" });

            migrationBuilder.CreateIndex(
                name: "IX_Placements_AwardedOn",
                table: "Placements",
                column: "AwardedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_AwardedOn",
                table: "Penalties",
                column: "AwardedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Bonuses_AwardedOn",
                table: "Bonuses",
                column: "AwardedOn");
        }
    }
}
