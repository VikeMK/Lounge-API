using System;

namespace Lounge.Web.Stats
{
    public record EventData(int Id, int NumTeams, int NumPlayers, string Tier, DateTime VerifiedOn);
}
