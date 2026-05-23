using F1Fantasy.Repository;

namespace F1Fantasy.Services;

/// <summary>
/// Enum representing the types of data that can be cached
/// </summary>
public enum DataType
{
    Results,
    Qualifying,
    DriverStandings,
    ConstructorStandings,
    Races,
    Circuits,
    Drivers,
    Constructors
}

/// <summary>
/// Centralized service for determining whether cached data should be refreshed.
/// Implements smart caching logic: metadata tracking, time-based expiration, and race schedule checking.
/// </summary>
public class CacheStalenessService
{
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly RaceRepository _raceRepository;
    private readonly ILogger<CacheStalenessService> _logger;

    public CacheStalenessService(
        DataFetchMetadataRepository metadataRepository,
        RaceRepository raceRepository,
        ILogger<CacheStalenessService> logger)
    {
        _metadataRepository = metadataRepository;
        _raceRepository = raceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Determines if data should be fetched from API based on cache staleness.
    /// </summary>
    /// <param name="season">The season year</param>
    /// <param name="dataType">The type of data being fetched</param>
    /// <param name="options">Optional configuration for staleness detection</param>
    /// <returns>True if data should be fetched from API, false if cache is still valid</returns>
    public async Task<bool> ShouldFetchAsync(
        string season, 
        DataType dataType, 
        CacheStalenessOptions? options = null)
    {
        options ??= CacheStalenessOptions.Default;

        // Check metadata for last fetch (convert enum to string for database)
        var metadata = await _metadataRepository.GetMetadataAsync(season, dataType.ToString());
        
        if (metadata == null || !metadata.FetchSuccessful)
        {
            _logger.LogDebug("No valid metadata for {DataType}/{Season}, should fetch", dataType, season);
            return true;
        }
        
        // Time-based expiration
        var currentYear = DateTime.UtcNow.Year;
        if (!int.TryParse(season, out var seasonYear))
        {
            _logger.LogWarning("Season '{Season}' is not a valid year, treating as current season", season);
            seasonYear = currentYear;
        }
        var cacheExpiration = seasonYear < currentYear
            ? options.PastSeasonExpiration 
            : options.CurrentSeasonExpiration;
        
        var age = DateTime.UtcNow - metadata.LastFetchedAt;
        if (age > cacheExpiration)
        {
            _logger.LogDebug("{DataType} cache expired for season {Season} (age: {Age}), should fetch", 
                dataType, season, age);
            return true;
        }
        
        // Check if there might be new data since last fetch (based on race schedule)
        if (options.CheckRaceSchedule)
        {
            var races = await _raceRepository.GetBySeasonAsync(season);
            var racesSinceLastFetch = races
                .Where(r => DateTime.TryParse(r.Date, out var raceDate) &&
                           raceDate > metadata.LastFetchedAt &&
                           raceDate.Add(options.RaceDataAvailabilityBuffer) < DateTime.UtcNow)
                .ToList();
            
            if (racesSinceLastFetch.Any())
            {
                _logger.LogInformation("Found {Count} race(s) since last fetch for {DataType}/{Season}, should fetch", 
                    racesSinceLastFetch.Count, dataType, season);
                return true;
            }
        }
        
        _logger.LogDebug("{DataType} cache valid for season {Season}, skip fetch", dataType, season);
        return false;
    }
}

/// <summary>
/// Configuration options for cache staleness detection
/// </summary>
public class CacheStalenessOptions
{
    /// <summary>
    /// How long cached data is valid for current season
    /// </summary>
    public TimeSpan CurrentSeasonExpiration { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// How long cached data is valid for past seasons (data is final)
    /// </summary>
    public TimeSpan PastSeasonExpiration { get; set; } = TimeSpan.FromDays(7);
    
    /// <summary>
    /// Whether to check race schedule for new events since last fetch
    /// </summary>
    public bool CheckRaceSchedule { get; set; } = true;
    
    /// <summary>
    /// Buffer time after race date when data becomes available.
    /// - Qualifying: Available same day (TimeSpan.Zero)
    /// - Results: Available ~2 hours after race (TimeSpan.FromHours(2))
    /// - Standings: Available after results (TimeSpan.FromDays(1))
    /// </summary>
    public TimeSpan RaceDataAvailabilityBuffer { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Default options: 1hr current season, 7 days past, check race schedule, 1 day buffer
    /// </summary>
    public static CacheStalenessOptions Default => new();
    
    /// <summary>
    /// Options for qualifying data (available immediately after qualifying session)
    /// </summary>
    public static CacheStalenessOptions ForQualifying => new()
    {
        CurrentSeasonExpiration = TimeSpan.FromHours(1),
        PastSeasonExpiration = TimeSpan.FromDays(7),
        CheckRaceSchedule = true,
        RaceDataAvailabilityBuffer = TimeSpan.Zero // Qualifying happens before race
    };
    
    /// <summary>
    /// Options for race results (available shortly after race)
    /// </summary>
    public static CacheStalenessOptions ForResults => new()
    {
        CurrentSeasonExpiration = TimeSpan.FromHours(1),
        PastSeasonExpiration = TimeSpan.FromDays(7),
        CheckRaceSchedule = true,
        RaceDataAvailabilityBuffer = TimeSpan.FromDays(1) // Results available within a day
    };
    
    /// <summary>
    /// Options for standings (updated after results are finalized)
    /// </summary>
    public static CacheStalenessOptions ForStandings => new()
    {
        CurrentSeasonExpiration = TimeSpan.FromHours(1),
        PastSeasonExpiration = TimeSpan.FromDays(7),
        CheckRaceSchedule = true,
        RaceDataAvailabilityBuffer = TimeSpan.FromDays(1) // Standings updated after race
    };
}
