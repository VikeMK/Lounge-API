using Lounge.Web.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Lounge.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Table> Tables => Set<Table>();
        public DbSet<TableScore> TableScores => Set<TableScore>();
        public DbSet<Penalty> Penalties => Set<Penalty>();
        public DbSet<Bonus> Bonuses => Set<Bonus>();
        public DbSet<PlayerStat> PlayerStats => Set<PlayerStat>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>().ToTable("Players");
            modelBuilder.Entity<Table>().ToTable("Tables");
            modelBuilder.Entity<TableScore>().ToTable("TableScores");
            modelBuilder.Entity<Penalty>().ToTable("Penalties");
            modelBuilder.Entity<Bonus>().ToTable("Bonuses");

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.MKCId)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.NormalizedName)
                .IsUnique();

            modelBuilder.Entity<TableScore>()
                .HasKey(t => new { t.TableId, t.PlayerId });

            modelBuilder.Entity<Penalty>()
                .HasIndex(p => p.AwardedOn);

            modelBuilder.Entity<Bonus>()
                .HasIndex(p => p.AwardedOn);

            modelBuilder.Entity<PlayerStat>()
                .HasNoKey()
                .ToView("View_PlayerStats");

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
