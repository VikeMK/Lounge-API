using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddChangeTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 4 HOURS, AUTO_CLEANUP = ON)", true);
            var tablesToEnableChangeTracking = new string[] { "Bonuses", "Penalties", "Placements", "Players", "PlayerSeasonData", "Tables", "TableScores" };
            foreach (var table in tablesToEnableChangeTracking)
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE CHANGE_TRACKING", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET CHANGE_TRACKING = OFF", true);
        }
    }
}
