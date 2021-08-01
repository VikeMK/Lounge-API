using Lounge.Web.Models;
using Lounge.Web.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;

namespace Lounge.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IOptions<LoungeSettings> loungeSettingsOptions;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IOptions<LoungeSettings> loungeSettingsOptions)
            : base(options)
        {
            this.loungeSettingsOptions = loungeSettingsOptions;
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<PlayerSeasonData> PlayerSeasonData => Set<PlayerSeasonData>();
        public DbSet<Table> Tables => Set<Table>();
        public DbSet<TableScore> TableScores => Set<TableScore>();
        public DbSet<Penalty> Penalties => Set<Penalty>();
        public DbSet<Bonus> Bonuses => Set<Bonus>();
        public DbSet<Placement> Placements => Set<Placement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var defaultSeason = loungeSettingsOptions.Value.Season;

            modelBuilder.Entity<Player>().ToTable("Players");
            modelBuilder.Entity<PlayerSeasonData>().ToTable("PlayerSeasonData");
            modelBuilder.Entity<Table>().ToTable("Tables");
            modelBuilder.Entity<TableScore>().ToTable("TableScores");
            modelBuilder.Entity<Penalty>().ToTable("Penalties");
            modelBuilder.Entity<Bonus>().ToTable("Bonuses");
            modelBuilder.Entity<Placement>().ToTable("Placements");

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.MKCId)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.DiscordId)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.NormalizedName)
                .IsUnique();

            modelBuilder.Entity<PlayerSeasonData>()
                .Property(psd => psd.Season)
                .HasDefaultValue(defaultSeason);

            modelBuilder.Entity<PlayerSeasonData>()
                .HasKey(psd => new { psd.PlayerId, psd.Season });

            modelBuilder.Entity<PlayerSeasonData>()
                .HasIndex(psd => new { psd.Season, psd.Mmr });

            modelBuilder.Entity<Table>()
                .Property(t => t.Season)
                .HasDefaultValue(defaultSeason);

            modelBuilder.Entity<TableScore>()
                .HasKey(t => new { t.TableId, t.PlayerId });

            modelBuilder.Entity<Penalty>()
                .HasIndex(p => p.AwardedOn);

            modelBuilder.Entity<Penalty>()
                .Property(p => p.Season)
                .HasDefaultValue(defaultSeason);

            modelBuilder.Entity<Bonus>()
                .HasIndex(p => p.AwardedOn);

            modelBuilder.Entity<Bonus>()
                .Property(b => b.Season)
                .HasDefaultValue(defaultSeason);

            modelBuilder.Entity<Placement>()
                .HasIndex(p => p.AwardedOn);

            modelBuilder.Entity<Placement>()
                .Property(p => p.Season)
                .HasDefaultValue(defaultSeason);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        modelBuilder.Entity(entityType.ClrType)
                         .Property<DateTime>(property.Name)
                         .HasConversion(
                          v => v.ToUniversalTime(),
                          v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        modelBuilder.Entity(entityType.ClrType)
                         .Property<DateTime?>(property.Name)
                         .HasConversion(
                          v => v.HasValue ? v.Value.ToUniversalTime() : v,
                          v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
                    }
                }
            }
        }
    }
}
