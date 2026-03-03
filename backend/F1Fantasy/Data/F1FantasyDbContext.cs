using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Data;

public class F1FantasyDbContext : DbContext
{
    public F1FantasyDbContext(DbContextOptions<F1FantasyDbContext> options)
        : base(options)
    {
    }

    // F1 Data DbSets
    public DbSet<Race> Races { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Circuit> Circuits { get; set; }
    public DbSet<Constructor> Constructors { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<Qualifying> Qualifyings { get; set; }
    public DbSet<PitStop> PitStops { get; set; }
    public DbSet<LapTiming> LapTimings { get; set; }
    public DbSet<DriverStanding> DriverStandings { get; set; }
    public DbSet<ConstructorStanding> ConstructorStandings { get; set; }
    public DbSet<Status> Statuses { get; set; }
    
    // Metadata DbSets
    public DbSet<DataFetchMetadata> DataFetchMetadata { get; set; }

    // Fantasy League DbSets
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<ConstructorChampionshipPrediction> ConstructorChampionshipPredictions { get; set; }
    public DbSet<DriverChampionshipPrediction> DriverChampionshipPredictions { get; set; }
    public DbSet<DriverDraftPrediction> DriverDraftPredictions { get; set; }
    public DbSet<DestructorPrediction> DestructorPredictions { get; set; }
    public DbSet<MrSaturdayPrediction> MrSaturdayPredictions { get; set; }
    public DbSet<ZeroPointerPrediction> ZeroPointerPredictions { get; set; }
    public DbSet<WildcardPrediction> WildcardPredictions { get; set; }
    public DbSet<Standing> Standings { get; set; }
    
    // Cache DbSets
    public DbSet<UserDisplayNameCache> UserDisplayNameCache { get; set; }

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
            entity.Property(r => r.StatusId).HasMaxLength(10); // Foreign key to Status table
            entity.Property(r => r.IsSprint).IsRequired().HasDefaultValue(false);
            
            // Index for common queries
            entity.HasIndex(r => new { r.Season, r.Round });
            entity.HasIndex(r => new { r.Season, r.Round, r.IsSprint });
            entity.HasIndex(r => r.DriverId);
            entity.HasIndex(r => r.ConstructorId);
            entity.HasIndex(r => r.StatusId); // Index for status lookups
            
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

        // LapTiming configuration
        modelBuilder.Entity<LapTiming>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Season).HasMaxLength(10).IsRequired();
            entity.Property(l => l.Round).HasMaxLength(10).IsRequired();
            entity.Property(l => l.LapNumber).HasMaxLength(10).IsRequired();
            entity.Property(l => l.DriverId).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Position).HasMaxLength(10).IsRequired();
            entity.Property(l => l.Time).HasMaxLength(20);
            
            // Index for common queries
            entity.HasIndex(l => new { l.Season, l.Round });
            entity.HasIndex(l => new { l.Season, l.Round, l.LapNumber });
            entity.HasIndex(l => new { l.Season, l.Round, l.DriverId });
            entity.HasIndex(l => l.DriverId);
        });

        // DriverStanding configuration
        modelBuilder.Entity<DriverStanding>(entity =>
        {
            entity.HasKey(ds => new { ds.Season, ds.DriverId });
            entity.Property(ds => ds.Season).HasMaxLength(10);
            entity.Property(ds => ds.DriverId).HasMaxLength(100);
            entity.Property(ds => ds.Round).HasMaxLength(10).IsRequired();
            entity.Property(ds => ds.Position).HasMaxLength(10).IsRequired();
            entity.Property(ds => ds.PositionText).HasMaxLength(10).IsRequired();
            entity.Property(ds => ds.Points).HasMaxLength(10).IsRequired();
            entity.Property(ds => ds.Wins).HasMaxLength(10).IsRequired();
            entity.Property(ds => ds.ConstructorId).HasMaxLength(100).IsRequired();
            
            // Index for common queries
            entity.HasIndex(ds => ds.Season);
            entity.HasIndex(ds => ds.DriverId);
            
            // Ignore navigation properties
            entity.Ignore(ds => ds.Driver);
            entity.Ignore(ds => ds.Constructor);
        });

        // ConstructorStanding configuration
        modelBuilder.Entity<ConstructorStanding>(entity =>
        {
            entity.HasKey(cs => new { cs.Season, cs.ConstructorId });
            entity.Property(cs => cs.Season).HasMaxLength(10);
            entity.Property(cs => cs.ConstructorId).HasMaxLength(100);
            entity.Property(cs => cs.Round).HasMaxLength(10).IsRequired();
            entity.Property(cs => cs.Position).HasMaxLength(10).IsRequired();
            entity.Property(cs => cs.PositionText).HasMaxLength(10).IsRequired();
            entity.Property(cs => cs.Points).HasMaxLength(10).IsRequired();
            entity.Property(cs => cs.Wins).HasMaxLength(10).IsRequired();
            
            // Index for common queries
            entity.HasIndex(cs => cs.Season);
            entity.HasIndex(cs => cs.ConstructorId);
            
            // Ignore navigation property
            entity.Ignore(cs => cs.Constructor);
        });

        // Status configuration
        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(s => s.StatusId);
            entity.Property(s => s.StatusId).HasMaxLength(10);
            entity.Property(s => s.StatusText).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Count).HasMaxLength(10).IsRequired();
            
            // Index for text lookups
            entity.HasIndex(s => s.StatusText);
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(200).IsRequired();
            entity.Property(g => g.InviteCode).HasMaxLength(50).IsRequired();
            entity.Property(g => g.LockMode).HasMaxLength(20).IsRequired();
            entity.Property(g => g.AdminUserId).HasMaxLength(100).IsRequired();
            
            entity.HasIndex(g => g.InviteCode).IsUnique();
            entity.HasIndex(g => g.AdminUserId);
        });

        // GroupMember configuration
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(gm => gm.Id);
            entity.Property(gm => gm.UserId).HasMaxLength(100).IsRequired();
            
            entity.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();
            entity.HasIndex(gm => gm.UserId);
            
            entity.HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ConstructorChampionshipPrediction configuration
        modelBuilder.Entity<ConstructorChampionshipPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            
            // Store ranked constructor IDs as JSON column
            entity.Property(p => p.RankedConstructorIds)
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DriverChampionshipPrediction configuration
        modelBuilder.Entity<DriverChampionshipPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            
            // Store ranked driver IDs as JSON column
            entity.Property(p => p.RankedDriverIds)
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DriverDraftPrediction configuration
        modelBuilder.Entity<DriverDraftPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Driver1Id).HasMaxLength(100);
            entity.Property(p => p.Driver2Id).HasMaxLength(100);
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DestructorPrediction configuration
        modelBuilder.Entity<DestructorPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Driver1Id).HasMaxLength(100);
            entity.Property(p => p.Driver2Id).HasMaxLength(100);
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MrSaturdayPrediction configuration
        modelBuilder.Entity<MrSaturdayPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Driver1Id).HasMaxLength(100);
            entity.Property(p => p.Driver2Id).HasMaxLength(100);
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ZeroPointerPrediction configuration
        modelBuilder.Entity<ZeroPointerPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.DriverIds)
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WildcardPrediction configuration
        modelBuilder.Entity<WildcardPrediction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Statement).HasMaxLength(500);
            
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.HasIndex(p => p.UserId);
            
            entity.HasOne(p => p.Group)
                .WithMany()
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Standing configuration
        modelBuilder.Entity<Standing>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.UserId).HasMaxLength(100).IsRequired();
            
            entity.HasIndex(s => new { s.GroupId, s.UserId }).IsUnique();
            entity.HasIndex(s => new { s.GroupId, s.Rank });
            entity.HasIndex(s => s.UserId);
            
            entity.HasOne(s => s.Group)
                .WithMany()
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DataFetchMetadata configuration
        modelBuilder.Entity<DataFetchMetadata>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Season).HasMaxLength(10).IsRequired();
            entity.Property(d => d.DataType).HasMaxLength(50).IsRequired();
            entity.Property(d => d.ErrorMessage).HasMaxLength(500);
            
            entity.HasIndex(d => new { d.Season, d.DataType }).IsUnique();
        });
        
        // UserDisplayNameCache configuration
        modelBuilder.Entity<UserDisplayNameCache>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).HasMaxLength(100).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.CachedAt).IsRequired();
            entity.Property(u => u.ExpiresAt).IsRequired();
            
            entity.HasIndex(u => u.ExpiresAt);
        });
    }
}
