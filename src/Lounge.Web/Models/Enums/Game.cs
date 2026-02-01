using System;
using System.ComponentModel;

namespace Lounge.Web.Models.Enums
{
    public enum GameMode
    {
        [Description("Mario Kart 8 Deluxe")]
        mk8dx = 0,

        [Description("Mario Kart World")]
        mkworld = 1,

        [Description("Mario Kart World (12P)")]
        mkworld12p = 2,

        [Description("Mario Kart World (24P)")]
        mkworld24p = 3
    }

    public enum RegistrationGameMode
    {
        [Description("Mario Kart 8 Deluxe")]
        mk8dx = 0,
        [Description("Mario Kart World")]
        mkworld = 1
    }

    public static class GameExtensions
    {
        //private static readonly string[] GameModeIds = { "mk8dx", "mkworld", "mkworld12p", "mkworld24p" };

        public static string GetStringId(this GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.mk8dx => "mk8dx",
                GameMode.mkworld => "mkworld",
                GameMode.mkworld12p => "mkworld12p",
                GameMode.mkworld24p => "mkworld24p",
                _ => throw new ArgumentOutOfRangeException(nameof(gameMode), "Invalid game mode enum value."),
            };
        }

        public static RegistrationGameMode GetRegistrationGameMode(this GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.mkworld12p => RegistrationGameMode.mkworld,
                GameMode.mkworld24p => RegistrationGameMode.mkworld,
                GameMode.mkworld => RegistrationGameMode.mkworld,
                GameMode.mk8dx => RegistrationGameMode.mk8dx,
                _ => throw new ArgumentOutOfRangeException(nameof(gameMode), "Invalid game mode enum value."),
            };
        }

        public static string GetStringId(this RegistrationGameMode gameMode)
        {
            return gameMode switch
            {
                RegistrationGameMode.mk8dx => "mk8dx",
                RegistrationGameMode.mkworld => "mkworld",
                _ => throw new ArgumentOutOfRangeException(nameof(gameMode), "Invalid registration game mode enum value."),
            };
        }
    }
}
