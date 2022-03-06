using Lounge.Web.Data;
using Lounge.Web.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Lounge
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>(),
                serviceProvider.GetRequiredService<ILoungeSettingsService>());

            // put any data to seed here

            await context.SaveChangesAsync();
        }
    }
}
