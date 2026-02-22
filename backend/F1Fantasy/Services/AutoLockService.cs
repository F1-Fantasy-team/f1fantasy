using F1Fantasy.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace F1Fantasy.Services;

public class AutoLockService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoLockService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public AutoLockService(
        IServiceProvider serviceProvider,
        ILogger<AutoLockService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-lock service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndLockGroupsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-lock service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndLockGroupsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var groupRepository = scope.ServiceProvider.GetRequiredService<GroupRepository>();
        var raceRepository = scope.ServiceProvider.GetRequiredService<RaceRepository>();

        // Get current season's first race date
        var currentYear = DateTime.UtcNow.Year;
        var firstRace = (await raceRepository.GetBySeasonAsync(currentYear.ToString()))
            .OrderBy(r => r.Date)
            .FirstOrDefault();

        if (firstRace == null)
        {
            _logger.LogWarning("No races found for season {Year}", currentYear);
            return;
        }

        // Parse first race date
        if (!DateTime.TryParse(firstRace.Date, out var firstRaceDate))
        {
            _logger.LogWarning("Could not parse race date: {Date}", firstRace.Date);
            return;
        }

        // Check if first race has started
        if (DateTime.UtcNow < firstRaceDate)
        {
            return; // Not time to lock yet
        }

        // Get all groups with system or hybrid mode that aren't locked
        var allGroups = await groupRepository.GetAllGroupsAsync();
        var groupsToLock = allGroups.Where(g =>
            !g.PredictionsLocked &&
            (g.LockMode == "system" || g.LockMode == "hybrid")
        ).ToList();

        if (groupsToLock.Any())
        {
            _logger.LogInformation(
                "Auto-locking {Count} groups for season {Year}",
                groupsToLock.Count,
                currentYear);

            foreach (var group in groupsToLock)
            {
                await groupRepository.SetPredictionsLockedAsync(group.Id, true);
                _logger.LogInformation(
                    "Auto-locked group {GroupId} ({GroupName})",
                    group.Id,
                    group.Name);
            }
        }
    }
}
