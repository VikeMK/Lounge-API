using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Lounge.Web.Models.Enums
{
    public enum Game
    {
        [Description("Mario Kart 8 Deluxe")]
        [EnumMember(Value = "mk8dx")]
        MK8DX = 0,

        [Description("Mario Kart World")]
        [EnumMember(Value = "mkworld")]
        MKWorld = 1,
    }

    public static class GameExtensions
    {
        private static readonly string[] GameIds = { "mk8dx", "mkworld" };
        public static string GetStringId(this Game game)
        {
            if ((int)game < 0 || (int)game >= GameIds.Length)
                throw new ArgumentOutOfRangeException(nameof(game), "Invalid game enum value.");
            return GameIds[(int)game];
        }
    }
}
