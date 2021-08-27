using System;

namespace Lounge.Web.Stats
{
    public record EventData(int Id, int NumTeams, string Tier, DateTime VerifiedOn);
}
