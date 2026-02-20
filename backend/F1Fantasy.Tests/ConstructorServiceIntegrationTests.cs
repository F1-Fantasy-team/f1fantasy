using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for ConstructorService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can actually fetch and parse constructor data from the real API,
/// handle pagination correctly, and store data in PostgreSQL database
/// </summary>
public class ConstructorServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ConstructorRepository _constructorRepository;
    private readonly ConstructorService _constructorService;
    private readonly PaginationStateTracker _paginationState;

    public ConstructorServiceIntegrationTests()
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
        _constructorRepository = new ConstructorRepository(context);
        _paginationState = new PaginationStateTracker();
        _constructorService = new ConstructorService(_httpClient, _constructorRepository, _paginationState);
    }

    [Fact]
    public async Task GetAllConstructorsAsync_FetchesAndParsesAllConstructors()
    {
        // Arrange & Act
        var constructors = await _constructorService.GetAllConstructorsAsync();

        // Assert
        constructors.Should().NotBeNull();
        constructors.Should().NotBeEmpty("F1 has had many constructors since 1950");
        
        var constructorList = constructors.ToList();
        constructorList.Should().HaveCountGreaterThan(80, "F1 has had over 80 different constructors in the API");

        // Verify first constructor structure
        var firstConstructor = constructorList.First();
        firstConstructor.ConstructorId.Should().NotBeNullOrEmpty();
        firstConstructor.Name.Should().NotBeNullOrEmpty();
        firstConstructor.Nationality.Should().NotBeNullOrEmpty();
        firstConstructor.Url.Should().NotBeNullOrEmpty();
        firstConstructor.Url.Should().Contain("wikipedia", "constructor URLs point to Wikipedia");
    }

    [Fact]
    public async Task GetAllConstructorsAsync_HandlesPaginationCorrectly()
    {
        // Arrange
        await _constructorRepository.ClearAsync();

        // Act
        var constructors = await _constructorService.GetAllConstructorsAsync();

        // Assert
        var constructorList = constructors.ToList();
        
        // API returns 30 items per page, total should be > 30 to verify pagination worked
        constructorList.Should().HaveCountGreaterThan(30, "pagination should fetch more than one page");
        
        // Verify no duplicate constructor IDs (would indicate pagination errors)
        var constructorIds = constructorList.Select(c => c.ConstructorId).ToList();
        constructorIds.Should().OnlyHaveUniqueItems("pagination should not create duplicates");
    }

    [Fact]
    public async Task GetAllConstructorsAsync_ParsesConstructorDataCorrectly()
    {
        // Arrange & Act
        var constructors = await _constructorService.GetAllConstructorsAsync();

        // Assert
        var constructorList = constructors.ToList();
        
        // Verify each constructor has required data parsed
        foreach (var constructor in constructorList)
        {
            constructor.ConstructorId.Should().NotBeNullOrEmpty("constructorId should be parsed");
            constructor.Name.Should().NotBeNullOrEmpty("name should be parsed");
            constructor.Nationality.Should().NotBeNullOrEmpty("nationality should be parsed");
            constructor.Url.Should().NotBeNullOrEmpty("url should be parsed");
            constructor.Url.Should().StartWith("http", "URL should be valid");
        }
    }

    [Fact]
    public async Task GetAllConstructorsAsync_StoresConstructorsInRepository()
    {
        // Arrange
        await _constructorRepository.ClearAsync();

        // Act
        var constructors = await _constructorService.GetAllConstructorsAsync();

        // Assert
        var repositoryConstructors = (await _constructorRepository.GetAllAsync()).ToList();
        repositoryConstructors.Should().HaveCount(constructors.Count(), "all fetched constructors should be stored in repository");
    }

    [Fact]
    public async Task GetConstructorByIdAsync_Ferrari_RetrievesCorrectConstructor()
    {
        // Arrange
        var constructorId = "ferrari";
        
        // Act
        var constructor = await _constructorService.GetConstructorByIdAsync(constructorId);

        // Assert
        constructor.Should().NotBeNull();
        constructor!.ConstructorId.Should().Be(constructorId);
        constructor.Name.Should().Be("Ferrari");
        constructor.Nationality.Should().Be("Italian");
    }

    [Fact]
    public async Task GetConstructorByIdAsync_Mercedes_RetrievesCorrectConstructor()
    {
        // Arrange
        var constructorId = "mercedes";
        
        // Act
        var constructor = await _constructorService.GetConstructorByIdAsync(constructorId);

        // Assert
        constructor.Should().NotBeNull();
        constructor!.ConstructorId.Should().Be(constructorId);
        constructor.Name.Should().Contain("Mercedes");
        constructor.Nationality.Should().Be("German");
    }

    [Fact]
    public async Task GetConstructorByIdAsync_UsesRepositoryCache()
    {
        // Arrange
        await _constructorRepository.ClearAsync();
        var constructorId = "red_bull";
        
        // First call should fetch from API
        await _constructorService.GetConstructorByIdAsync(constructorId);
        
        // Verify it's cached
        var cachedBefore = await _constructorRepository.GetByConstructorIdAsync(constructorId);
        cachedBefore.Should().NotBeNull("constructor should be cached after first call");

        // Act - Second call should use cache
        var constructor = await _constructorService.GetConstructorByIdAsync(constructorId);

        // Assert
        constructor.Should().NotBeNull();
        constructor!.ConstructorId.Should().Be(constructorId);
        constructor.Should().Be(cachedBefore, "should return the same cached instance");
    }

    [Fact]
    public async Task GetCachedConstructors_AfterFetchingAll_ReturnsAllConstructors()
    {
        // Arrange
        await _constructorRepository.ClearAsync();
        await _constructorService.GetAllConstructorsAsync();

        // Act
        var cachedConstructors = (await _constructorService.GetCachedConstructors()).ToList();

        // Assert
        cachedConstructors.Should().NotBeEmpty();
        cachedConstructors.Should().HaveCountGreaterThan(80, "should have all fetched constructors in cache");
    }

    [Fact]
    public async Task GetAllConstructorsAsync_ValidatesJsonStructure_ParsesMRDataCorrectly()
    {
        // Arrange & Act
        var constructors = await _constructorService.GetAllConstructorsAsync();

        // Assert - This verifies the JSON parsing works correctly
        constructors.Should().NotBeNull();
        constructors.Should().BeAssignableTo<IEnumerable<Constructor>>();
        
        var firstConstructor = constructors.FirstOrDefault();
        firstConstructor.Should().NotBeNull();
        
        // Verify the nested JSON structure was parsed (MRData -> ConstructorTable -> Constructors)
        firstConstructor!.Name.Should().NotBeNullOrEmpty("the API response MRData.ConstructorTable.Constructors should be parsed correctly");
    }

    [Fact]
    public async Task GetAllConstructorsAsync_IncludesWellKnownConstructors()
    {
        // Arrange & Act
        var constructors = await _constructorService.GetAllConstructorsAsync();

        // Assert
        var constructorList = constructors.ToList();
        var constructorIds = constructorList.Select(c => c.ConstructorId).ToList();
        
        // Verify famous constructors are included
        constructorIds.Should().Contain("ferrari", "Ferrari should be included");
        constructorIds.Should().Contain("mclaren", "McLaren should be included");
        constructorIds.Should().Contain("mercedes", "Mercedes should be included");
    }

    [Fact]
    public async Task GetConstructorsBySeasonAsync_2026Season_ReturnsCurrentConstructors()
    {
        // Arrange
        var season = "2026";

        // Act
        var constructors = await _constructorService.GetConstructorsBySeasonAsync(season);

        // Assert
        var constructorList = constructors.ToList();
        constructorList.Should().NotBeEmpty("2026 season has constructors");
        
        // Verify data structure
        foreach (var constructor in constructorList)
        {
            constructor.ConstructorId.Should().NotBeNullOrEmpty();
            constructor.Name.Should().NotBeNullOrEmpty();
            constructor.Nationality.Should().NotBeNullOrEmpty();
            constructor.Url.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetConstructorsBySeasonAsync_2024Season_ReturnsConstructors()
    {
        // Arrange
        var season = "2024";

        // Act
        var constructors = await _constructorService.GetConstructorsBySeasonAsync(season);

        // Assert
        var constructorList = constructors.ToList();
        constructorList.Should().NotBeEmpty("2024 season had 10 constructors");
        constructorList.Should().HaveCount(10, "2024 F1 season had exactly 10 constructors");
    }

    [Fact]
    public async Task GetConstructorsBySeasonAsync_StoresInRepository()
    {
        // Arrange
        await _constructorRepository.ClearAsync();
        var season = "2024";

        // Act
        var constructors = await _constructorService.GetConstructorsBySeasonAsync(season);

        // Assert
        var repositoryConstructors = (await _constructorRepository.GetAllAsync()).ToList();
        repositoryConstructors.Should().HaveCount(constructors.Count(), "fetched constructors should be stored in repository");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
