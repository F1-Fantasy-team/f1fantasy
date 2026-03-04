using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for PostgreSQL database operations
/// These tests verify CRUD operations work correctly with the actual database
/// and clean up test data after each test
/// </summary>
public class DatabaseIntegrationTests : IAsyncLifetime
{
    private F1FantasyDbContext _context = null!;
    private RaceRepository _raceRepository = null!;
    private SeasonRepository _seasonRepository = null!;
    private CircuitRepository _circuitRepository = null!;
    private ConstructorRepository _constructorRepository = null!;
    private DriverRepository _driverRepository = null!;

    private readonly List<string> _testSeasons = new();
    private readonly List<(string season, string round)> _testRaces = new();
    private readonly List<string> _testCircuits = new();
    private readonly List<string> _testConstructors = new();
    private readonly List<string> _testDrivers = new();

    public async Task InitializeAsync()
    {
        // Load environment variables for connection string
        var envPath = @"C:\Projects\f1fantasy\backend\.env";
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
        
        // Get connection string directly from environment variable (loaded by DotNetEnv)
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Database connection string not found. Ensure .env file exists at {envPath} and contains ConnectionStrings__DefaultConnection");
        }

        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new F1FantasyDbContext(options);
        
        // Ensure database is created and migrated
        await _context.Database.MigrateAsync();

        // Initialize repositories
        _raceRepository = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        _seasonRepository = new SeasonRepository(_context);
        _circuitRepository = new CircuitRepository(_context);
        _constructorRepository = new ConstructorRepository(_context, NullLogger<ConstructorRepository>.Instance);
        _driverRepository = new DriverRepository(_context, NullLogger<DriverRepository>.Instance);
    }

    public async Task DisposeAsync()
    {
        // Clean up all test data in reverse order of dependencies
        
        // Clean up races
        foreach (var (season, round) in _testRaces)
        {
            var race = await _context.Races.FirstOrDefaultAsync(r => r.Season == season && r.Round == round);
            if (race != null)
            {
                _context.Races.Remove(race);
            }
        }

        // Clean up seasons
        foreach (var year in _testSeasons)
        {
            var season = await _context.Seasons.FirstOrDefaultAsync(s => s.Year == year);
            if (season != null)
            {
                _context.Seasons.Remove(season);
            }
        }

        // Clean up circuits
        foreach (var circuitId in _testCircuits)
        {
            var circuit = await _context.Circuits.FirstOrDefaultAsync(c => c.CircuitId == circuitId);
            if (circuit != null)
            {
                _context.Circuits.Remove(circuit);
            }
        }

        // Clean up constructors
        foreach (var constructorId in _testConstructors)
        {
            var constructor = await _context.Constructors.FirstOrDefaultAsync(c => c.ConstructorId == constructorId);
            if (constructor != null)
            {
                _context.Constructors.Remove(constructor);
            }
        }

        // Clean up drivers
        foreach (var driverId in _testDrivers)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
            }
        }

        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    #region Season Repository Tests

    [Fact]
    public async Task SeasonRepository_AddOrUpdate_CreatesNewSeason()
    {
        // Arrange
        var testYear = "9999";
        _testSeasons.Add(testYear);
        
        var season = new Season
        {
            Year = testYear,
            Url = "https://test.com/season/9999"
        };

        // Act
        await _seasonRepository.AddOrUpdateAsync(season);

        // Assert
        var retrieved = await _seasonRepository.GetByYearAsync(testYear);
        retrieved.Should().NotBeNull();
        retrieved!.Year.Should().Be(testYear);
        retrieved.Url.Should().Be(season.Url);
    }

    [Fact]
    public async Task SeasonRepository_AddOrUpdate_UpdatesExistingSeason()
    {
        // Arrange
        var testYear = "9998";
        _testSeasons.Add(testYear);
        
        var season = new Season { Year = testYear, Url = "https://test.com/original" };
        await _seasonRepository.AddOrUpdateAsync(season);

        // Act - Update
        season.Url = "https://test.com/updated";
        await _seasonRepository.AddOrUpdateAsync(season);

        // Assert
        var retrieved = await _seasonRepository.GetByYearAsync(testYear);
        retrieved.Should().NotBeNull();
        retrieved!.Url.Should().Be("https://test.com/updated");
    }

    [Fact]
    public async Task SeasonRepository_GetAll_ReturnsAllSeasons()
    {
        // Arrange
        var testYear1 = "9997";
        var testYear2 = "9996";
        _testSeasons.Add(testYear1);
        _testSeasons.Add(testYear2);

        await _seasonRepository.AddOrUpdateAsync(new Season { Year = testYear1, Url = "https://test.com/1" });
        await _seasonRepository.AddOrUpdateAsync(new Season { Year = testYear2, Url = "https://test.com/2" });

        // Act
        var allSeasons = await _seasonRepository.GetAllAsync();

        // Assert
        var seasonsList = allSeasons.ToList();
        seasonsList.Should().Contain(s => s.Year == testYear1);
        seasonsList.Should().Contain(s => s.Year == testYear2);
    }

    #endregion

    #region Circuit Repository Tests

    [Fact]
    public async Task CircuitRepository_AddOrUpdate_CreatesNewCircuit()
    {
        // Arrange
        var circuitId = "test_circuit_1";
        _testCircuits.Add(circuitId);
        
        var circuit = new Circuit
        {
            CircuitId = circuitId,
            CircuitName = "Test Circuit",
            Url = "https://test.com/circuit",
            Location = new Location
            {
                Lat = "12.345",
                Long = "67.890",
                Locality = "Test City",
                Country = "Test Country"
            }
        };

        // Act
        await _circuitRepository.AddOrUpdateAsync(circuit);

        // Assert
        var retrieved = await _circuitRepository.GetByCircuitIdAsync(circuitId);
        retrieved.Should().NotBeNull();
        retrieved!.CircuitId.Should().Be(circuitId);
        retrieved.CircuitName.Should().Be("Test Circuit");
        retrieved.Location.Country.Should().Be("Test Country");
    }

    [Fact]
    public async Task CircuitRepository_AddOrUpdate_UpdatesExistingCircuit()
    {
        // Arrange
        var circuitId = "test_circuit_2";
        _testCircuits.Add(circuitId);
        
        var circuit = new Circuit
        {
            CircuitId = circuitId,
            CircuitName = "Original Name",
            Url = "https://test.com/circuit",
            Location = new Location { Lat = "1", Long = "2", Locality = "City", Country = "Country" }
        };
        await _circuitRepository.AddOrUpdateAsync(circuit);

        // Act - Update
        circuit.CircuitName = "Updated Name";
        circuit.Location.Country = "New Country";
        await _circuitRepository.AddOrUpdateAsync(circuit);

        // Assert
        var retrieved = await _circuitRepository.GetByCircuitIdAsync(circuitId);
        retrieved.Should().NotBeNull();
        retrieved!.CircuitName.Should().Be("Updated Name");
        retrieved.Location.Country.Should().Be("New Country");
    }

    [Fact]
    public async Task CircuitRepository_GetAll_ReturnsAllCircuits()
    {
        // Arrange
        var circuitId1 = "test_circuit_3";
        var circuitId2 = "test_circuit_4";
        _testCircuits.Add(circuitId1);
        _testCircuits.Add(circuitId2);

        await _circuitRepository.AddOrUpdateAsync(new Circuit
        {
            CircuitId = circuitId1,
            CircuitName = "Circuit 1",
            Url = "https://test.com/1",
            Location = new Location { Lat = "1", Long = "2", Locality = "City1", Country = "Country1" }
        });
        await _circuitRepository.AddOrUpdateAsync(new Circuit
        {
            CircuitId = circuitId2,
            CircuitName = "Circuit 2",
            Url = "https://test.com/2",
            Location = new Location { Lat = "3", Long = "4", Locality = "City2", Country = "Country2" }
        });

        // Act
        var allCircuits = await _circuitRepository.GetAllAsync();

        // Assert
        var circuitsList = allCircuits.ToList();
        circuitsList.Should().Contain(c => c.CircuitId == circuitId1);
        circuitsList.Should().Contain(c => c.CircuitId == circuitId2);
    }

    #endregion

    #region Constructor Repository Tests

    [Fact]
    public async Task ConstructorRepository_AddOrUpdate_CreatesNewConstructor()
    {
        // Arrange
        var constructorId = "test_constructor_1";
        _testConstructors.Add(constructorId);
        
        var constructor = new Constructor
        {
            ConstructorId = constructorId,
            Name = "Test Racing",
            Url = "https://test.com/constructor",
            Nationality = "British"
        };

        // Act
        await _constructorRepository.AddOrUpdateAsync(constructor);

        // Assert
        var retrieved = await _constructorRepository.GetByConstructorIdAsync(constructorId);
        retrieved.Should().NotBeNull();
        retrieved!.ConstructorId.Should().Be(constructorId);
        retrieved.Name.Should().Be("Test Racing");
        retrieved.Nationality.Should().Be("British");
    }

    [Fact]
    public async Task ConstructorRepository_AddOrUpdate_UpdatesExistingConstructor()
    {
        // Arrange
        var constructorId = "test_constructor_2";
        _testConstructors.Add(constructorId);
        
        var constructor = new Constructor
        {
            ConstructorId = constructorId,
            Name = "Original Team",
            Url = "https://test.com/constructor",
            Nationality = "Italian"
        };
        await _constructorRepository.AddOrUpdateAsync(constructor);

        // Act - Update
        constructor.Name = "Updated Team";
        constructor.Nationality = "German";
        await _constructorRepository.AddOrUpdateAsync(constructor);

        // Assert
        var retrieved = await _constructorRepository.GetByConstructorIdAsync(constructorId);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Team");
        retrieved.Nationality.Should().Be("German");
    }

    #endregion

    #region Driver Repository Tests

    [Fact]
    public async Task DriverRepository_AddOrUpdate_CreatesNewDriver()
    {
        // Arrange
        var driverId = "test_driver_1";
        _testDrivers.Add(driverId);
        
        var driver = new Driver
        {
            DriverId = driverId,
            PermanentNumber = "99",
            Code = "TST",
            GivenName = "Test",
            FamilyName = "Driver",
            DateOfBirth = "1990-01-01",
            Nationality = "British",
            Url = "https://test.com/driver"
        };

        // Act
        await _driverRepository.AddOrUpdateAsync(driver);

        // Assert
        var retrieved = await _driverRepository.GetByDriverIdAsync(driverId);
        retrieved.Should().NotBeNull();
        retrieved!.DriverId.Should().Be(driverId);
        retrieved.GivenName.Should().Be("Test");
        retrieved.FamilyName.Should().Be("Driver");
        retrieved.Code.Should().Be("TST");
    }

    [Fact]
    public async Task DriverRepository_AddOrUpdate_UpdatesExistingDriver()
    {
        // Arrange
        var driverId = "test_driver_2";
        _testDrivers.Add(driverId);
        
        var driver = new Driver
        {
            DriverId = driverId,
            PermanentNumber = "98",
            Code = "OLD",
            GivenName = "Original",
            FamilyName = "Name",
            DateOfBirth = "1990-01-01",
            Nationality = "British",
            Url = "https://test.com/driver"
        };
        await _driverRepository.AddOrUpdateAsync(driver);

        // Act - Update
        driver.Code = "NEW";
        driver.GivenName = "Updated";
        await _driverRepository.AddOrUpdateAsync(driver);

        // Assert
        var retrieved = await _driverRepository.GetByDriverIdAsync(driverId);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("NEW");
        retrieved.GivenName.Should().Be("Updated");
    }

    [Fact]
    public async Task DriverRepository_GetAll_ReturnsAllDrivers()
    {
        // Arrange
        var driverId1 = "test_driver_3";
        var driverId2 = "test_driver_4";
        _testDrivers.Add(driverId1);
        _testDrivers.Add(driverId2);

        await _driverRepository.AddOrUpdateAsync(new Driver
        {
            DriverId = driverId1,
            PermanentNumber = "97",
            Code = "DR1",
            GivenName = "Alice",
            FamilyName = "Aaa",
            DateOfBirth = "1990-01-01",
            Nationality = "British",
            Url = "https://test.com/1"
        });
        await _driverRepository.AddOrUpdateAsync(new Driver
        {
            DriverId = driverId2,
            PermanentNumber = "96",
            Code = "DR2",
            GivenName = "Bob",
            FamilyName = "Bbb",
            DateOfBirth = "1991-01-01",
            Nationality = "German",
            Url = "https://test.com/2"
        });

        // Act
        var allDrivers = await _driverRepository.GetAllAsync();

        // Assert
        var driversList = allDrivers.ToList();
        driversList.Should().Contain(d => d.DriverId == driverId1);
        driversList.Should().Contain(d => d.DriverId == driverId2);
    }

    #endregion

    #region Race Repository Tests

    [Fact]
    public async Task RaceRepository_AddOrUpdate_CreatesNewRace()
    {
        // Arrange
        var season = "9995";
        var round = "1";
        _testRaces.Add((season, round));
        
        var race = new Race
        {
            Season = season,
            Round = round,
            RaceName = "Test Grand Prix",
            Url = "https://test.com/race",
            Date = "2026-03-01",
            Time = "14:00:00Z",
            Circuit = new Circuit
            {
                CircuitId = "test_gp",
                CircuitName = "Test Circuit",
                Url = "https://test.com/circuit",
                Location = new Location
                {
                    Lat = "12.345",
                    Long = "67.890",
                    Locality = "Test City",
                    Country = "Test Country"
                }
            }
        };

        // Act
        await _raceRepository.AddOrUpdateAsync(race);

        // Assert
        var retrieved = await _raceRepository.GetByRoundAsync(season, round);
        retrieved.Should().NotBeNull();
        retrieved!.RaceName.Should().Be("Test Grand Prix");
        retrieved.Season.Should().Be(season);
        retrieved.Round.Should().Be(round);
    }

    [Fact]
    public async Task RaceRepository_AddOrUpdate_UpdatesExistingRace()
    {
        // Arrange
        var season = "9994";
        var round = "1";
        _testRaces.Add((season, round));
        
        var race = new Race
        {
            Season = season,
            Round = round,
            RaceName = "Original GP",
            Url = "https://test.com/race",
            Date = "2026-03-01",
            Time = "14:00:00Z",
            Circuit = new Circuit
            {
                CircuitId = "test",
                CircuitName = "Test",
                Url = "https://test.com",
                Location = new Location { Lat = "1", Long = "2", Locality = "City", Country = "Country" }
            }
        };
        await _raceRepository.AddOrUpdateAsync(race);

        // Act - Update
        race.RaceName = "Updated GP";
        await _raceRepository.AddOrUpdateAsync(race);

        // Assert
        var retrieved = await _raceRepository.GetByRoundAsync(season, round);
        retrieved.Should().NotBeNull();
        retrieved!.RaceName.Should().Be("Updated GP");
    }

    [Fact]
    public async Task RaceRepository_GetBySeason_ReturnsRacesForSeason()
    {
        // Arrange
        var season = "9993";
        _testRaces.Add((season, "1"));
        _testRaces.Add((season, "2"));

        await _raceRepository.AddOrUpdateAsync(new Race
        {
            Season = season,
            Round = "1",
            RaceName = "Race 1",
            Url = "https://test.com/1",
            Date = "2026-03-01",
            Time = "14:00:00Z",
            Circuit = new Circuit
            {
                CircuitId = "test1",
                CircuitName = "Test",
                Url = "https://test.com",
                Location = new Location { Lat = "1", Long = "2", Locality = "City", Country = "Country" }
            }
        });
        await _raceRepository.AddOrUpdateAsync(new Race
        {
            Season = season,
            Round = "2",
            RaceName = "Race 2",
            Url = "https://test.com/2",
            Date = "2026-03-15",
            Time = "14:00:00Z",
            Circuit = new Circuit
            {
                CircuitId = "test2",
                CircuitName = "Test",
                Url = "https://test.com",
                Location = new Location { Lat = "1", Long = "2", Locality = "City", Country = "Country" }
            }
        });

        // Act
        var races = await _raceRepository.GetBySeasonAsync(season);

        // Assert
        var racesList = races.ToList();
        racesList.Should().HaveCount(2);
        racesList.Should().Contain(r => r.Round == "1");
        racesList.Should().Contain(r => r.Round == "2");
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task RepositoryClear_RemovesAllData()
    {
        // Arrange
        var testYear = "9992";
        _testSeasons.Add(testYear);
        await _seasonRepository.AddOrUpdateAsync(new Season { Year = testYear, Url = "https://test.com" });

        // Act
        await _seasonRepository.ClearAsync();

        // Assert
        var retrieved = await _seasonRepository.GetByYearAsync(testYear);
        retrieved.Should().BeNull();
        
        // Note: We don't need to track this for cleanup since we already cleared it
        _testSeasons.Remove(testYear);
    }

    #endregion
}
