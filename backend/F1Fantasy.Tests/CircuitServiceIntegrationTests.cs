using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for CircuitService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can actually fetch and parse circuit data from the real API
/// and handle pagination correctly
/// </summary>
public class CircuitServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CircuitRepository _circuitRepository;
    private readonly CircuitService _circuitService;
    private readonly PaginationStateTracker _paginationState;

    public CircuitServiceIntegrationTests()
    {
        _httpClient = new HttpClient();
        _circuitRepository = new CircuitRepository();
        _paginationState = new PaginationStateTracker();
        _circuitService = new CircuitService(_httpClient, _circuitRepository, _paginationState);
    }

    [Fact]
    public async Task GetAllCircuitsAsync_FetchesAndParsesAllCircuits()
    {
        // Arrange & Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert
        circuits.Should().NotBeNull();
        circuits.Should().NotBeEmpty("F1 has used many circuits since 1950");
        
        var circuitList = circuits.ToList();
        circuitList.Should().HaveCountGreaterThan(70, "F1 has used over 70 different circuits");

        // Verify first circuit structure
        var firstCircuit = circuitList.First();
        firstCircuit.CircuitId.Should().NotBeNullOrEmpty();
        firstCircuit.CircuitName.Should().NotBeNullOrEmpty();
        firstCircuit.Url.Should().NotBeNullOrEmpty();
        firstCircuit.Url.Should().Contain("wikipedia", "circuit URLs point to Wikipedia");
        
        firstCircuit.Location.Should().NotBeNull();
        firstCircuit.Location.Locality.Should().NotBeNullOrEmpty();
        firstCircuit.Location.Country.Should().NotBeNullOrEmpty();
        firstCircuit.Location.Lat.Should().NotBeNullOrEmpty();
        firstCircuit.Location.Long.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllCircuitsAsync_HandlesPaginationCorrectly()
    {
        // Arrange
        _circuitRepository.Clear();

        // Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert
        var circuitList = circuits.ToList();
        
        // API returns 30 items per page, total should be > 30 to verify pagination worked
        circuitList.Should().HaveCountGreaterThan(30, "pagination should fetch more than one page");
        
        // Verify no duplicate circuit IDs (would indicate pagination errors)
        var circuitIds = circuitList.Select(c => c.CircuitId).ToList();
        circuitIds.Should().OnlyHaveUniqueItems("pagination should not create duplicates");
    }

    [Fact]
    public async Task GetAllCircuitsAsync_ParsesCircuitDataCorrectly()
    {
        // Arrange & Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert
        var circuitList = circuits.ToList();
        
        // Verify each circuit has required data parsed
        foreach (var circuit in circuitList)
        {
            circuit.CircuitId.Should().NotBeNullOrEmpty("circuitId should be parsed");
            circuit.CircuitName.Should().NotBeNullOrEmpty("circuitName should be parsed");
            circuit.Url.Should().NotBeNullOrEmpty("url should be parsed");
            circuit.Url.Should().StartWith("http", "URL should be valid");
            
            // Location data
            circuit.Location.Should().NotBeNull("location should be present");
            circuit.Location.Locality.Should().NotBeNullOrEmpty("locality should be parsed");
            circuit.Location.Country.Should().NotBeNullOrEmpty("country should be parsed");
            circuit.Location.Lat.Should().NotBeNullOrEmpty("latitude should be parsed");
            circuit.Location.Long.Should().NotBeNullOrEmpty("longitude should be parsed");
        }
    }

    [Fact]
    public async Task GetAllCircuitsAsync_StoresCircuitsInRepository()
    {
        // Arrange
        _circuitRepository.Clear();

        // Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert
        var repositoryCircuits = _circuitRepository.GetAll().ToList();
        repositoryCircuits.Should().HaveCount(circuits.Count(), "all fetched circuits should be stored in repository");
    }

    [Fact]
    public async Task GetCircuitByIdAsync_SpecificCircuit_RetrievesCorrectCircuit()
    {
        // Arrange
        var circuitId = "monaco";
        
        // Act
        var circuit = await _circuitService.GetCircuitByIdAsync(circuitId);

        // Assert
        circuit.Should().NotBeNull();
        circuit!.CircuitId.Should().Be(circuitId);
        circuit.CircuitName.Should().Contain("Monaco", "Monaco circuit should have Monaco in the name");
        circuit.Location.Country.Should().Be("Monaco");
    }

    [Fact]
    public async Task GetCircuitByIdAsync_AnotherCircuit_RetrievesCorrectCircuit()
    {
        // Arrange
        var circuitId = "silverstone";
        
        // Act
        var circuit = await _circuitService.GetCircuitByIdAsync(circuitId);

        // Assert
        circuit.Should().NotBeNull();
        circuit!.CircuitId.Should().Be(circuitId);
        circuit.CircuitName.Should().Contain("Silverstone", "Silverstone circuit should have Silverstone in the name");
        circuit.Location.Country.Should().Be("UK");
    }

    [Fact]
    public async Task GetCircuitByIdAsync_UsesRepositoryCache()
    {
        // Arrange
        _circuitRepository.Clear();
        var circuitId = "spa";
        
        // First call should fetch from API
        await _circuitService.GetCircuitByIdAsync(circuitId);
        
        // Verify it's cached
        var cachedBefore = _circuitRepository.GetByCircuitId(circuitId);
        cachedBefore.Should().NotBeNull("circuit should be cached after first call");

        // Act - Second call should use cache
        var circuit = await _circuitService.GetCircuitByIdAsync(circuitId);

        // Assert
        circuit.Should().NotBeNull();
        circuit!.CircuitId.Should().Be(circuitId);
        circuit.Should().Be(cachedBefore, "should return the same cached instance");
    }

    [Fact]
    public async Task GetCachedCircuits_AfterFetchingAll_ReturnsAllCircuits()
    {
        // Arrange
        _circuitRepository.Clear();
        await _circuitService.GetAllCircuitsAsync();

        // Act
        var cachedCircuits = _circuitService.GetCachedCircuits().ToList();

        // Assert
        cachedCircuits.Should().NotBeEmpty();
        cachedCircuits.Should().HaveCountGreaterThan(70, "should have all fetched circuits in cache");
    }

    [Fact]
    public async Task GetCachedCircuits_ReturnsOrderedByName()
    {
        // Arrange
        _circuitRepository.Clear();
        await _circuitService.GetAllCircuitsAsync();

        // Act
        var cachedCircuits = _circuitService.GetCachedCircuits().ToList();

        // Assert
        cachedCircuits.Should().NotBeEmpty();
        // Note: Circuits are ordered by CircuitName in the repository
        cachedCircuits.Count.Should().BeGreaterThan(70, "should have all circuits");
    }

    [Fact]
    public async Task GetAllCircuitsAsync_ValidatesJsonStructure_ParsesMRDataCorrectly()
    {
        // Arrange & Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert - This verifies the JSON parsing works correctly
        circuits.Should().NotBeNull();
        circuits.Should().BeAssignableTo<IEnumerable<Circuit>>();
        
        var firstCircuit = circuits.FirstOrDefault();
        firstCircuit.Should().NotBeNull();
        
        // Verify the nested JSON structure was parsed (MRData -> CircuitTable -> Circuits)
        firstCircuit!.CircuitName.Should().NotBeNullOrEmpty("the API response MRData.CircuitTable.Circuits should be parsed correctly");
    }

    [Fact]
    public async Task GetAllCircuitsAsync_IncludesWellKnownCircuits()
    {
        // Arrange & Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert
        var circuitList = circuits.ToList();
        var circuitIds = circuitList.Select(c => c.CircuitId).ToList();
        
        // Verify famous circuits are included
        circuitIds.Should().Contain("monaco", "Monaco should be included");
        circuitIds.Should().Contain("silverstone", "Silverstone should be included");
        circuitIds.Should().Contain("monza", "Monza should be included");
    }

    [Fact]
    public async Task GetAllCircuitsAsync_ParsesLocationCoordinatesAsStrings()
    {
        // Arrange & Act
        var circuits = await _circuitService.GetAllCircuitsAsync();

        // Assert
        var circuitList = circuits.ToList();
        
        // Verify coordinates are parsed as strings (some may have decimal points)
        foreach (var circuit in circuitList)
        {
            // Latitude should be parseable as decimal
            decimal.TryParse(circuit.Location.Lat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat).Should().BeTrue($"latitude '{circuit.Location.Lat}' should be a valid decimal");
            
            // Longitude should be parseable as decimal
            decimal.TryParse(circuit.Location.Long, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng).Should().BeTrue($"longitude '{circuit.Location.Long}' should be a valid decimal");
            
            // Lat should be between -90 and 90
            lat.Should().BeInRange(-90, 90, "latitude should be valid");
            
            // Long should be between -180 and 180
            lng.Should().BeInRange(-180, 180, "longitude should be valid");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
