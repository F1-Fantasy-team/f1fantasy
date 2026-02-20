using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Data;

public class F1FantasyDbContext : DbContext
{
    public F1FantasyDbContext(DbContextOptions<F1FantasyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Race> Races { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Circuit> Circuits { get; set; }
    public DbSet<Constructor> Constructors { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<Qualifying> Qualifyings { get; set; }
    public DbSet<PitStop> PitStops { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Season configuration
        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasKey(s => s.Year);
            entity.Property(s => s.Year).HasMaxLength(10);
            entity.Property(s => s.Url).HasMaxLength(500);
        });

        // Circuit configuration
        modelBuilder.Entity<Circuit>(entity =>
        {
            entity.HasKey(c => c.CircuitId);
            entity.Property(c => c.CircuitId).HasMaxLength(100);
            entity.Property(c => c.CircuitName).HasMaxLength(200);
            entity.Property(c => c.Url).HasMaxLength(500);
            
            entity.OwnsOne(c => c.Location, location =>
            {
                location.Property(l => l.Lat).HasMaxLength(50);
                location.Property(l => l.Long).HasMaxLength(50);
                location.Property(l => l.Locality).HasMaxLength(200);
                location.Property(l => l.Country).HasMaxLength(200);
            });
        });

        // Constructor configuration
        modelBuilder.Entity<Constructor>(entity =>
        {
            entity.HasKey(c => c.ConstructorId);
            entity.Property(c => c.ConstructorId).HasMaxLength(100);
            entity.Property(c => c.Name).HasMaxLength(200);
            entity.Property(c => c.Url).HasMaxLength(500);
            entity.Property(c => c.Nationality).HasMaxLength(100);
        });

        // Driver configuration
        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(d => d.DriverId);
            entity.Property(d => d.DriverId).HasMaxLength(100);
            entity.Property(d => d.PermanentNumber).HasMaxLength(10);
            entity.Property(d => d.Code).HasMaxLength(10);
            entity.Property(d => d.GivenName).HasMaxLength(100);
            entity.Property(d => d.FamilyName).HasMaxLength(100);
            entity.Property(d => d.DateOfBirth).HasMaxLength(50);
            entity.Property(d => d.Nationality).HasMaxLength(100);
            entity.Property(d => d.Url).HasMaxLength(500);
        });

        // Race configuration
        modelBuilder.Entity<Race>(entity =>
        {
            entity.HasKey(r => new { r.Season, r.Round });
            entity.Property(r => r.Season).HasMaxLength(10);
            entity.Property(r => r.Round).HasMaxLength(10);
            entity.Property(r => r.RaceName).HasMaxLength(200);
            entity.Property(r => r.Url).HasMaxLength(500);
            entity.Property(r => r.Date).HasMaxLength(50);
            entity.Property(r => r.Time).HasMaxLength(50);
            
            // Ignore the Circuit navigation property - we'll store circuit data as JSON or owned entity
            entity.Ignore(r => r.Circuit);
            
            entity.OwnsOne(r => r.FirstPractice, session =>
            {
                session.Property(s => s.Date).HasMaxLength(50);
                session.Property(s => s.Time).HasMaxLength(50);
            });
            entity.OwnsOne(r => r.SecondPractice, session =>
            {
                session.Property(s => s.Date).HasMaxLength(50);
                session.Property(s => s.Time).HasMaxLength(50);
            });
            entity.OwnsOne(r => r.ThirdPractice, session =>
            {
                session.Property(s => s.Date).HasMaxLength(50);
                session.Property(s => s.Time).HasMaxLength(50);
            });
            entity.OwnsOne(r => r.Qualifying, session =>
            {
                session.Property(s => s.Date).HasMaxLength(50);
                session.Property(s => s.Time).HasMaxLength(50);
            });
            entity.OwnsOne(r => r.Sprint, session =>
            {
                session.Property(s => s.Date).HasMaxLength(50);
                session.Property(s => s.Time).HasMaxLength(50);
            });
            entity.OwnsOne(r => r.SprintQualifying, session =>
            {
                session.Property(s => s.Date).HasMaxLength(50);
                session.Property(s => s.Time).HasMaxLength(50);
            });
        });

        // Result configuration
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Season).HasMaxLength(10).IsRequired();
            entity.Property(r => r.Round).HasMaxLength(10).IsRequired();
            entity.Property(r => r.Number).HasMaxLength(10);
            entity.Property(r => r.Position).HasMaxLength(10);
            entity.Property(r => r.PositionText).HasMaxLength(10);
            entity.Property(r => r.Points).HasMaxLength(10);
            entity.Property(r => r.DriverId).HasMaxLength(100).IsRequired();
            entity.Property(r => r.ConstructorId).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Grid).HasMaxLength(10);
            entity.Property(r => r.Laps).HasMaxLength(10);
            entity.Property(r => r.Status).HasMaxLength(100);
            entity.Property(r => r.IsSprint).IsRequired().HasDefaultValue(false);
            
            // Index for common queries
            entity.HasIndex(r => new { r.Season, r.Round });
            entity.HasIndex(r => new { r.Season, r.Round, r.IsSprint });
            entity.HasIndex(r => r.DriverId);
            entity.HasIndex(r => r.ConstructorId);
            
            // Ignore navigation properties - we don't want to load full driver/constructor objects
            entity.Ignore(r => r.Driver);
            entity.Ignore(r => r.Constructor);
            
            // Owned entities for nested objects
            entity.OwnsOne(r => r.Time, time =>
            {
                time.Property(t => t.Millis).HasMaxLength(50);
                time.Property(t => t.Time).HasMaxLength(50);
            });
            
            entity.OwnsOne(r => r.FastestLap, fastestLap =>
            {
                fastestLap.Property(f => f.Rank).HasMaxLength(10);
                fastestLap.Property(f => f.Lap).HasMaxLength(10);
                
                fastestLap.OwnsOne(f => f.Time, lapTime =>
                {
                    lapTime.Property(t => t.Time).HasMaxLength(50);
                });
                
                fastestLap.OwnsOne(f => f.AverageSpeed, avgSpeed =>
                {
                    avgSpeed.Property(a => a.Units).HasMaxLength(10);
                    avgSpeed.Property(a => a.Speed).HasMaxLength(20);
                });
            });
        });

        // Qualifying configuration
        modelBuilder.Entity<Qualifying>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Season).HasMaxLength(10).IsRequired();
            entity.Property(q => q.Round).HasMaxLength(10).IsRequired();
            entity.Property(q => q.Number).HasMaxLength(10);
            entity.Property(q => q.Position).HasMaxLength(10);
            entity.Property(q => q.DriverId).HasMaxLength(100).IsRequired();
            entity.Property(q => q.ConstructorId).HasMaxLength(100).IsRequired();
            entity.Property(q => q.Q1).HasMaxLength(20);
            entity.Property(q => q.Q2).HasMaxLength(20);
            entity.Property(q => q.Q3).HasMaxLength(20);
            
            // Index for common queries
            entity.HasIndex(q => new { q.Season, q.Round });
            entity.HasIndex(q => q.DriverId);
            entity.HasIndex(q => q.ConstructorId);
            
            // Ignore navigation properties
            entity.Ignore(q => q.Driver);
            entity.Ignore(q => q.Constructor);
        });

        // PitStop configuration
        modelBuilder.Entity<PitStop>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Season).HasMaxLength(10).IsRequired();
            entity.Property(p => p.Round).HasMaxLength(10).IsRequired();
            entity.Property(p => p.DriverId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Lap).HasMaxLength(10).IsRequired();
            entity.Property(p => p.Stop).HasMaxLength(10).IsRequired();
            entity.Property(p => p.Time).HasMaxLength(20);
            entity.Property(p => p.Duration).HasMaxLength(20);
            
            // Index for common queries
            entity.HasIndex(p => new { p.Season, p.Round });
            entity.HasIndex(p => new { p.Season, p.Round, p.DriverId });
            entity.HasIndex(p => p.DriverId);
        });
    }
}
