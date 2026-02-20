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
    }
}
