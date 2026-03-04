using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class DataFetchMetadataRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<DataFetchMetadataRepository> _logger;

    public DataFetchMetadataRepository(F1FantasyDbContext context, ILogger<DataFetchMetadataRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DataFetchMetadata?> GetMetadataAsync(string season, string dataType)
    {
        return await _context.DataFetchMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Season == season && m.DataType == dataType);
    }

    public async Task RecordFetchAsync(string season, string dataType, int? latestRound, bool success, string? errorMessage = null)
    {
        var existing = await GetMetadataAsync(season, dataType);
        
        if (existing != null)
        {
            existing.LastFetchedAt = DateTime.UtcNow;
            existing.LatestRoundAtFetch = latestRound;
            existing.FetchSuccessful = success;
            existing.ErrorMessage = errorMessage;
            _context.DataFetchMetadata.Update(existing);
        }
        else
        {
            await _context.DataFetchMetadata.AddAsync(new DataFetchMetadata
            {
                Season = season,
                DataType = dataType,
                LastFetchedAt = DateTime.UtcNow,
                LatestRoundAtFetch = latestRound,
                FetchSuccessful = success,
                ErrorMessage = errorMessage
            });
        }
        
        await _context.SaveChangesAsync();
        _logger.LogDebug("Recorded fetch metadata for {DataType} in season {Season}: Round {Round}, Success: {Success}", 
            dataType, season, latestRound, success);
    }

    public async Task<bool> ShouldFetchAsync(string season, string dataType, TimeSpan cacheExpiration)
    {
        var metadata = await GetMetadataAsync(season, dataType);
        
        if (metadata == null)
        {
            _logger.LogDebug("No fetch metadata found for {DataType}/{Season}, should fetch", dataType, season);
            return true;
        }
        
        if (!metadata.FetchSuccessful)
        {
            _logger.LogDebug("Previous fetch failed for {DataType}/{Season}, should retry", dataType, season);
            return true;
        }
        
        var age = DateTime.UtcNow - metadata.LastFetchedAt;
        if (age > cacheExpiration)
        {
            _logger.LogDebug("Cache expired for {DataType}/{Season} (age: {Age}), should fetch", dataType, season, age);
            return true;
        }
        
        _logger.LogDebug("Cache valid for {DataType}/{Season} (age: {Age}), skip fetch", dataType, season, age);
        return false;
    }
}
