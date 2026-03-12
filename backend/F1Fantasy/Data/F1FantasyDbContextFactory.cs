using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace F1Fantasy.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// This allows EF tools to create a DbContext without running the full application.
/// </summary>
public class F1FantasyDbContextFactory : IDesignTimeDbContextFactory<F1FantasyDbContext>
{
    public F1FantasyDbContext CreateDbContext(string[] args)
    {
        // Load environment variables from .env file
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        // Get connection string from environment
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string not found. Ensure .env file exists at {envPath} and contains ConnectionStrings__DefaultConnection");
        }

        var optionsBuilder = new DbContextOptionsBuilder<F1FantasyDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new F1FantasyDbContext(optionsBuilder.Options);
    }
}
