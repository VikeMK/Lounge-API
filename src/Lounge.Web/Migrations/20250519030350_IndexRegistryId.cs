using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lounge.Web.Migrations
{
    /// <inheritdoc />
    public partial class IndexRegistryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_MKCId",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RegistryId",
                table: "Players",
                column: "RegistryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_RegistryId",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_MKCId",
                table: "Players",
                column: "MKCId",
                unique: true);
        }
    }
}
