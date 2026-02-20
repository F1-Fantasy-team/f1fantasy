using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for DriverService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can actually fetch and parse driver data from the real API,
/// handle pagination correctly, and store data in PostgreSQL database
/// </summary>
[Collection("Sequential Integration Tests")]
public class DriverServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DriverRepository _driverRepository;
    private readonly DriverService _driverService;
    private readonly PaginationStateTracker _paginationState;

    public DriverServiceIntegrationTests()
    {
        _httpClient = new HttpClient();
        
        // Load environment variables from .env file
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
        var context = new F1FantasyDbContext(options);
        _driverRepository = new DriverRepository(context, NullLogger<DriverRepository>.Instance);
        _paginationState = new PaginationStateTracker();
        _driverService = new DriverService(_httpClient, _driverRepository, _paginationState, NullLogger<DriverService>.Instance);
    }

    [Fact]
    public async Task GetAllDriversAsync_FetchesAndParsesAllDrivers()
    {
        // Arrange & Act
        var drivers = await _driverService.GetAllDriversAsync();

        // Assert
        drivers.Should().NotBeNull();
        drivers.Should().NotBeEmpty("F1 has had many drivers since 1950");
        
        var driverList = drivers.ToList();
        driverList.Should().HaveCountGreaterThan(800, "F1 has had over 800 different drivers");

        // Verify first driver structure
        var firstDriver = driverList.First();
        firstDriver.DriverId.Should().NotBeNullOrEmpty();
        firstDriver.GivenName.Should().NotBeNullOrEmpty();
        firstDriver.FamilyName.Should().NotBeNullOrEmpty();
        firstDriver.Nationality.Should().NotBeNullOrEmpty();
        firstDriver.Url.Should().NotBeNullOrEmpty();
        firstDriver.Url.Should().Contain("wikipedia", "driver URLs point to Wikipedia");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetAllDriversAsync_HandlesPaginationCorrectly()
    {
        // Arrange
        await _driverRepository.ClearAsync();

        // Act
        var drivers = await _driverService.GetAllDriversAsync();

        // Assert
        var driverList = drivers.ToList();
        
        // API returns 30 items per page, total should be > 30 to verify pagination worked
        driverList.Should().HaveCountGreaterThan(30, "pagination should fetch more than one page");
        
        // Verify no duplicate driver IDs (would indicate pagination errors)
        var driverIds = driverList.Select(d => d.DriverId).ToList();
        driverIds.Should().OnlyHaveUniqueItems("pagination should not create duplicates");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetAllDriversAsync_ParsesDriverDataCorrectly()
    {
        // Arrange & Act
        var drivers = await _driverService.GetAllDriversAsync();

        // Assert
        var driverList = drivers.ToList();
        
        // Verify each driver has required data parsed
        foreach (var driver in driverList)
        {
            driver.DriverId.Should().NotBeNullOrEmpty("driverId should be parsed");
            driver.GivenName.Should().NotBeNullOrEmpty("givenName should be parsed");
            driver.FamilyName.Should().NotBeNullOrEmpty("familyName should be parsed");
            // Note: Some historical drivers may have empty nationality, URL, or DateOfBirth in API
        }
    }

    [Fact]
    public async Task GetAllDriversAsync_StoresDriversInRepository()
    {
        // Arrange
        await _driverRepository.ClearAsync();

        // Act
        var drivers = await _driverService.GetAllDriversAsync();

        // Assert
        var repositoryDrivers = (await _driverRepository.GetAllAsync()).ToList();
        repositoryDrivers.Should().HaveCount(drivers.Count(), "all fetched drivers should be stored in repository");
    }

    [Fact]
    public async Task GetDriverByIdAsync_Hamilton_RetrievesCorrectDriver()
    {
        // Arrange
        var driverId = "hamilton";
        
        // Act
        var driver = await _driverService.GetDriverByIdAsync(driverId);

        // Assert
        driver.Should().NotBeNull();
        driver!.DriverId.Should().Be(driverId);
        driver.FamilyName.Should().Be("Hamilton");
        driver.GivenName.Should().Be("Lewis");
        driver.Nationality.Should().Be("British");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetDriverByIdAsync_Verstappen_RetrievesCorrectDriver()
    {
        // Arrange
        var driverId = "max_verstappen";
        
        // Act
        var driver = await _driverService.GetDriverByIdAsync(driverId);

        // Assert
        driver.Should().NotBeNull();
        driver!.DriverId.Should().Be(driverId);
        driver.FamilyName.Should().Be("Verstappen");
        driver.GivenName.Should().Be("Max");
        driver.Nationality.Should().Be("Dutch");
    }

    [Fact]
    public async Task GetDriverByIdAsync_UsesRepositoryCache()
    {
        // Arrange
        await _driverRepository.ClearAsync();
        var driverId = "leclerc";
        
        // First call should fetch from API
        await _driverService.GetDriverByIdAsync(driverId);
        
        // Verify it's cached
        var cachedBefore = await _driverRepository.GetByDriverIdAsync(driverId);
        cachedBefore.Should().NotBeNull("driver should be cached after first call");

        // Act - Second call should use cache
        var driver = await _driverService.GetDriverByIdAsync(driverId);

        // Assert
        driver.Should().NotBeNull();
        driver!.DriverId.Should().Be(driverId);
        driver.Should().Be(cachedBefore, "should return the same cached instance");
    }

    [Fact]
    public async Task GetCachedDrivers_AfterFetchingAll_ReturnsAllDrivers()
    {
        // Arrange
        await _driverRepository.ClearAsync();
        await _driverService.GetAllDriversAsync();

        // Act
        var cachedDrivers = (await _driverService.GetCachedDriversAsync()).ToList();

        // Assert
        cachedDrivers.Should().NotBeEmpty();
        cachedDrivers.Should().HaveCountGreaterThan(800, "should have all fetched drivers in cache");
    }

    [Fact]
    public async Task GetAllDriversAsync_ValidatesJsonStructure_ParsesMRDataCorrectly()
    {
        // Arrange & Act
        var drivers = await _driverService.GetAllDriversAsync();

        // Assert - This verifies the JSON parsing works correctly
        drivers.Should().NotBeNull();
        drivers.Should().BeAssignableTo<IEnumerable<Driver>>();
        
        var firstDriver = drivers.FirstOrDefault();
        firstDriver.Should().NotBeNull();
        
        // Verify the nested JSON structure was parsed (MRData -> DriverTable -> Drivers)
        firstDriver!.FamilyName.Should().NotBeNullOrEmpty("the API response MRData.DriverTable.Drivers should be parsed correctly");
    }

    [Fact]
    public async Task GetAllDriversAsync_IncludesWellKnownDrivers()
    {
        // Arrange & Act
        var drivers = await _driverService.GetAllDriversAsync();

        // Assert
        var driverList = drivers.ToList();
        var driverIds = driverList.Select(d => d.DriverId).ToList();
        
        // Verify famous drivers are included
        driverIds.Should().Contain("hamilton", "Hamilton should be included");
        driverIds.Should().Contain("max_verstappen", "Verstappen should be included");
        driverIds.Should().Contain("alonso", "Alonso should be included");
    }

    [Fact]
    public async Task GetDriversBySeasonAsync_2026Season_ReturnsCurrentDrivers()
    {
        // Arrange
        var season = "2026";

        // Act
        var drivers = await _driverService.GetDriversBySeasonAsync(season);

        // Assert
        var driverList = drivers.ToList();
        driverList.Should().NotBeEmpty("2026 season has drivers");
        driverList.Should().HaveCountGreaterThanOrEqualTo(20, "2026 F1 season has at least 20 drivers");
        
        // Verify data structure
        foreach (var driver in driverList)
        {
            driver.DriverId.Should().NotBeNullOrEmpty();
            driver.GivenName.Should().NotBeNullOrEmpty();
            driver.FamilyName.Should().NotBeNullOrEmpty();
            driver.Nationality.Should().NotBeNullOrEmpty();
            // Note: Some drivers may have empty URL in API
            driver.Code.Should().NotBeNullOrEmpty("driver should have a 3-letter code");
            driver.Code.Length.Should().Be(3, "driver code should be exactly 3 letters");
        }
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetDriversBySeasonAsync_2024Season_ReturnsDrivers()
    {
        // Arrange
        var season = "2024";

        // Act
        var drivers = await _driverService.GetDriversBySeasonAsync(season);

        // Assert
        var driverList = drivers.ToList();
        driverList.Should().NotBeEmpty("2024 season had drivers");
        driverList.Should().HaveCountGreaterThanOrEqualTo(24, "2024 F1 season had at least 24 drivers (including mid-season changes)");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetDriversBySeasonAsync_StoresInRepository()
    {
        // Arrange
        await _driverRepository.ClearAsync();
        var season = "2026";

        // Act
        var drivers = await _driverService.GetDriversBySeasonAsync(season);

        // Assert
        var repositoryDrivers = (await _driverRepository.GetAllAsync()).ToList();
        repositoryDrivers.Should().HaveCount(drivers.Count(), "fetched drivers should be stored in repository");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
