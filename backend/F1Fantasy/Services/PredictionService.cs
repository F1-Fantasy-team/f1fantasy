using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Validation;

namespace F1Fantasy.Services;

public class PredictionService
{
    private readonly PredictionRepository _predictionRepository;
    private readonly GroupRepository _groupRepository;
    private readonly ConstructorService _constructorService;
    private readonly DriverService _driverService;

    public PredictionService(
        PredictionRepository predictionRepository,
        GroupRepository groupRepository,
        ConstructorService constructorService,
        DriverService driverService)
    {
        _predictionRepository = predictionRepository;
        _groupRepository = groupRepository;
        _constructorService = constructorService;
        _driverService = driverService;
    }

    private async Task ValidateGroupAndLockAsync(int groupId, string userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        if (!await _groupRepository.IsUserMemberAsync(groupId, userId))
        {
            throw new UnauthorizedAccessException("User is not a member of this group");
        }

        if (group.PredictionsLocked)
        {
            throw new InvalidOperationException("Predictions are locked for this group");
        }
    }

    // Constructor Championship
    public async Task<ConstructorChampionshipPrediction> SaveConstructorChampionshipAsync(
        int groupId, string userId, List<string> rankedConstructorIds)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        // Validate list size to prevent DoS attacks
        ValidationExtensions.ValidateListSize(rankedConstructorIds, 
            ValidationExtensions.MAX_CONSTRUCTOR_LIST_SIZE, "constructors");

        // Validate ID format for each constructor
        foreach (var id in rankedConstructorIds)
        {
            ValidationExtensions.ValidateId(id, "Constructor ID");
        }

        // Validate: Must have all active constructors for current season, no duplicates
        var activeConstructors = await _constructorService.GetActiveConstructorsAsync();
        var constructorIds = activeConstructors.Select(c => c.ConstructorId).ToList();

        if (rankedConstructorIds.Count != constructorIds.Count)
        {
            throw new ArgumentException($"Must rank all {constructorIds.Count} active constructors");
        }

        if (rankedConstructorIds.Distinct().Count() != rankedConstructorIds.Count)
        {
            throw new ArgumentException("Constructor IDs must be unique");
        }

        if (rankedConstructorIds.Any(id => !constructorIds.Contains(id)))
        {
            throw new ArgumentException("Invalid constructor IDs detected");
        }

        var prediction = new ConstructorChampionshipPrediction
        {
            GroupId = groupId,
            UserId = userId,
            RankedConstructorIds = rankedConstructorIds
        };

        return await _predictionRepository.UpsertConstructorChampionshipAsync(prediction);
    }

    public async Task<ConstructorChampionshipPrediction?> GetConstructorChampionshipAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetConstructorChampionshipAsync(groupId, userId);
    }

    // Driver Championship
    public async Task<DriverChampionshipPrediction> SaveDriverChampionshipAsync(
        int groupId, string userId, List<string> rankedDriverIds)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        // Validate list size to prevent DoS attacks
        ValidationExtensions.ValidateListSize(rankedDriverIds, 
            ValidationExtensions.MAX_DRIVER_LIST_SIZE, "drivers");

        // Validate ID format for each driver
        foreach (var id in rankedDriverIds)
        {
            ValidationExtensions.ValidateId(id, "Driver ID");
        }

        // Validate: Must have all active drivers (typically 20), no duplicates
        var activeDrivers = await _driverService.GetActiveDriversAsync();
        var driverIds = activeDrivers.Select(d => d.DriverId).ToList();
        var expectedCount = driverIds.Count;

        if (rankedDriverIds.Count != expectedCount)
        {
            throw new ArgumentException($"Must rank all {expectedCount} active drivers (found {rankedDriverIds.Count})");
        }

        if (rankedDriverIds.Distinct().Count() != rankedDriverIds.Count)
        {
            throw new ArgumentException("Driver IDs must be unique");
        }

        if (rankedDriverIds.Any(id => !driverIds.Contains(id)))
        {
            throw new ArgumentException("Invalid driver IDs detected");
        }

        var prediction = new DriverChampionshipPrediction
        {
            GroupId = groupId,
            UserId = userId,
            RankedDriverIds = rankedDriverIds
        };

        return await _predictionRepository.UpsertDriverChampionshipAsync(prediction);
    }

    public async Task<DriverChampionshipPrediction?> GetDriverChampionshipAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetDriverChampionshipAsync(groupId, userId);
    }

    // Driver Draft
    public async Task<DriverDraftPrediction> SaveDriverDraftAsync(
        int groupId, string userId, string? driver1Id, string? driver2Id)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        // Validate: Max 2 drivers, no duplicates
        if (driver1Id != null && driver2Id != null && driver1Id == driver2Id)
        {
            throw new ArgumentException("Cannot select the same driver twice");
        }

        var activeDrivers = await _driverService.GetActiveDriversAsync();
        var driverIds = activeDrivers.Select(d => d.DriverId).ToList();

        if (driver1Id != null && !driverIds.Contains(driver1Id))
        {
            throw new ArgumentException("Invalid driver1 ID - driver not active in current season");
        }

        if (driver2Id != null && !driverIds.Contains(driver2Id))
        {
            throw new ArgumentException("Invalid driver2 ID - driver not active in current season");
        }

        var prediction = new DriverDraftPrediction
        {
            GroupId = groupId,
            UserId = userId,
            Driver1Id = driver1Id,
            Driver2Id = driver2Id
        };

        return await _predictionRepository.UpsertDriverDraftAsync(prediction);
    }

    public async Task<DriverDraftPrediction?> GetDriverDraftAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetDriverDraftAsync(groupId, userId);
    }

    // Destructor
    public async Task<DestructorPrediction> SaveDestructorAsync(
        int groupId, string userId, string? driver1Id, string? driver2Id)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        if (driver1Id != null && driver2Id != null && driver1Id == driver2Id)
        {
            throw new ArgumentException("Cannot select the same driver twice");
        }

        var activeDrivers = await _driverService.GetActiveDriversAsync();
        var driverIds = activeDrivers.Select(d => d.DriverId).ToList();

        if (driver1Id != null && !driverIds.Contains(driver1Id))
        {
            throw new ArgumentException("Invalid driver1 ID - driver not active in current season");
        }

        if (driver2Id != null && !driverIds.Contains(driver2Id))
        {
            throw new ArgumentException("Invalid driver2 ID - driver not active in current season");
        }

        var prediction = new DestructorPrediction
        {
            GroupId = groupId,
            UserId = userId,
            Driver1Id = driver1Id,
            Driver2Id = driver2Id
        };

        return await _predictionRepository.UpsertDestructorAsync(prediction);
    }

    public async Task<DestructorPrediction?> GetDestructorAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetDestructorAsync(groupId, userId);
    }

    // Mr Saturday
    public async Task<MrSaturdayPrediction> SaveMrSaturdayAsync(
        int groupId, string userId, string? driver1Id, string? driver2Id)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        if (driver1Id != null && driver2Id != null && driver1Id == driver2Id)
        {
            throw new ArgumentException("Cannot select the same driver twice");
        }

        var activeDrivers = await _driverService.GetActiveDriversAsync();
        var driverIds = activeDrivers.Select(d => d.DriverId).ToList();

        if (driver1Id != null && !driverIds.Contains(driver1Id))
        {
            throw new ArgumentException("Invalid driver1 ID - driver not active in current season");
        }

        if (driver2Id != null && !driverIds.Contains(driver2Id))
        {
            throw new ArgumentException("Invalid driver2 ID - driver not active in current season");
        }

        var prediction = new MrSaturdayPrediction
        {
            GroupId = groupId,
            UserId = userId,
            Driver1Id = driver1Id,
            Driver2Id = driver2Id
        };

        return await _predictionRepository.UpsertMrSaturdayAsync(prediction);
    }

    public async Task<MrSaturdayPrediction?> GetMrSaturdayAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetMrSaturdayAsync(groupId, userId);
    }

    // Zero Pointer
    public async Task<ZeroPointerPrediction> SaveZeroPointerAsync(
        int groupId, string userId, List<string> driverIds)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        // Check for duplicates
        if (driverIds.Count != driverIds.Distinct().Count())
        {
            throw new ArgumentException("Cannot select the same driver multiple times");
        }

        // Validate all driver IDs exist and are active in current season
        var activeDrivers = await _driverService.GetActiveDriversAsync();
        var validDriverIds = activeDrivers.Select(d => d.DriverId).ToList();

        foreach (var driverId in driverIds)
        {
            if (!validDriverIds.Contains(driverId))
            {
                throw new ArgumentException($"Invalid driver ID: {driverId} - driver not active in current season");
            }
        }

        var prediction = new ZeroPointerPrediction
        {
            GroupId = groupId,
            UserId = userId,
            DriverIds = driverIds
        };

        return await _predictionRepository.UpsertZeroPointerAsync(prediction);
    }

    public async Task<ZeroPointerPrediction?> GetZeroPointerAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetZeroPointerAsync(groupId, userId);
    }

    // Wildcard
    public async Task<WildcardPrediction> SaveWildcardAsync(
        int groupId, string userId, string? statement)
    {
        await ValidateGroupAndLockAsync(groupId, userId);

        if (statement != null && statement.Length > 500)
        {
            throw new ArgumentException("Wildcard statement cannot exceed 500 characters");
        }

        var prediction = new WildcardPrediction
        {
            GroupId = groupId,
            UserId = userId,
            Statement = statement
        };

        return await _predictionRepository.UpsertWildcardAsync(prediction);
    }

    public async Task<WildcardPrediction?> GetWildcardAsync(int groupId, string userId)
    {
        return await _predictionRepository.GetWildcardAsync(groupId, userId);
    }

    public async Task<List<WildcardPrediction>> GetAllWildcardsAsync(int groupId, string userId)
    {
        // Verify user is a member of the group
        if (!await _groupRepository.IsUserMemberAsync(groupId, userId))
        {
            throw new UnauthorizedAccessException("User is not a member of this group");
        }

        return await _predictionRepository.GetAllWildcardsAsync(groupId);
    }
}
