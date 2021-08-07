using Lounge.Web.Models.Enums;
using System;

namespace Lounge.Web.Models.ViewModels
{
    public record Rank(Division Division, int? Level = null)
    {
        public string Name
        {
            get
            {
                var enumName = Enum.GetName(Division);
                if (enumName is null)
                    return "Unknown";

                return Level == null ? enumName : $"{enumName} {Level}";
            }
        }
    }
}
