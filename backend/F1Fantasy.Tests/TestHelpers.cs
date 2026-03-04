using F1Fantasy.Data;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Tests;

/// <summary>
/// Shared test helper classes used across multiple test files
/// </summary>
public class TestDbContextFactory : IDbContextFactory<F1FantasyDbContext>
{
    private readonly DbContextOptions<F1FantasyDbContext> _options;

    public TestDbContextFactory(DbContextOptions<F1FantasyDbContext> options)
    {
        _options = options;
    }

    public F1FantasyDbContext CreateDbContext()
    {
        return new F1FantasyDbContext(_options);
    }

    public async Task<F1FantasyDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new F1FantasyDbContext(_options));
    }
}
