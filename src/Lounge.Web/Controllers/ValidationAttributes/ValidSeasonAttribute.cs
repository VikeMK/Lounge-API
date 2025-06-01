using Lounge.Web.Models.Enums;
using Lounge.Web.Settings;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Lounge.Web.Controllers.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ValidSeasonAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            PropertyInfo? property = validationContext.ObjectType?.GetProperty("game");
            if (property is null)
            {
                return new ValidationResult("Game property not found in validation context.");
            }

            object? gameValue = property.GetValue(validationContext.ObjectInstance);
            if (gameValue is not Game game)
            {
                return new ValidationResult("Game property is not of type Game.");
            }

            var settingsService = (ILoungeSettingsService)validationContext.GetService(typeof(ILoungeSettingsService))!;

            var season = (int)value;
            var validSeasons = settingsService.ValidSeasons[game];
            foreach (var validSeason in validSeasons)
            {
                if (season == validSeason)
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult("Invalid Season");
        }
    }
}
