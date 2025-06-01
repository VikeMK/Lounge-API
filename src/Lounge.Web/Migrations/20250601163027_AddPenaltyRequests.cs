using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lounge.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PenaltyRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Game = table.Column<int>(type: "int", nullable: false),
                    PenaltyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TableId = table.Column<int>(type: "int", nullable: false),
                    NumberOfRaces = table.Column<int>(type: "int", nullable: false),
                    ReporterId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PenaltyRequests_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRequests_PlayerId",
                table: "PenaltyRequests",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyRequests_ReporterId",
                table: "PenaltyRequests",
                column: "ReporterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PenaltyRequests");
        }
    }
}
