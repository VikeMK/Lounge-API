using Lounge.Web.Settings;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;

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

            var settingsService = (ILoungeSettingsService)validationContext.GetService(typeof(ILoungeSettingsService))!;

            var season = (int)value;
            var validSeasons = settingsService.ValidSeasons;
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
