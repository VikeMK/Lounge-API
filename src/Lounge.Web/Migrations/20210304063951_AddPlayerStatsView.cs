using Microsoft.EntityFrameworkCore.Migrations;

namespace Lounge.Web.Migrations
{
    public partial class AddPlayerStatsView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW View_PlayerStats AS
SELECT
	CONVERT(INT, RANK() OVER(ORDER BY (CASE WHEN AllTime.EventsPlayed = 0 THEN 0 ELSE 1 END) DESC, Mmr DESC)) as Rank,
	Id,
	Name,
	Mmr,
	MaxMmr,
	NormalizedName,
	AllTime.EventsPlayed,
	AllTime.Wins,
	CASE WHEN AllTime.LargestGain < 0 THEN NULL ELSE AllTime.LargestGain END AS LargestGain,
	CASE WHEN AllTime.LargestLoss > 0 THEN NULL ELSE AllTime.LargestLoss END AS LargestLoss,
	LastTen.GainLoss AS LastTenGainLoss,
	LastTen.Wins AS LastTenWins,
	LastTen.Losses AS LastTenLosses
FROM Players p
OUTER APPLY (
	SELECT
		COUNT(*) as EventsPlayed,
		COALESCE(SUM(CASE WHEN s.NewMmr > s.PrevMmr THEN 1 ELSE 0 END), 0) AS Wins,
		MIN(s.NewMmr - s.PrevMmr) AS LargestLoss,
		MAX(s.NewMmr - s.PrevMmr) AS LargestGain
	FROM TableScores s
    JOIN Tables t
		ON t.Id = s.TableId
	WHERE s.PlayerId = p.Id
    AND t.DeletedOn IS NULL
	AND s.NewMmr IS NOT NULL) AllTime
OUTER APPLY (
	SELECT
		SUM(d.Delta) as GainLoss,
		COALESCE(SUM(CASE WHEN d.Delta > 0 THEN 1 ELSE 0 END), 0) AS Wins,
		COALESCE(SUM(CASE WHEN d.Delta > 0 THEN 0 ELSE 1 END), 0) AS Losses
	FROM (
		SELECT TOP 10
			s.NewMmr - s.PrevMmr AS Delta
		FROM TableScores s
		JOIN Tables t 
			ON t.Id = s.TableId
		WHERE s.PlayerId = p.Id AND s.NewMmr IS NOT NULL AND t.DeletedOn IS NULL
		ORDER BY t.VerifiedOn DESC) d) LastTen");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("DROP VIEW View_PlayerStats");
        }
    }
}
