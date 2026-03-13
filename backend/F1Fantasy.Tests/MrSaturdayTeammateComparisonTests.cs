using F1Fantasy.Models;
using F1Fantasy.Services;
using FluentAssertions;
using Xunit;
using System.Globalization;

namespace F1Fantasy.Tests;

/// <summary>
/// Tests for Mr Saturday teammate comparison logic using real 2026 Australian GP data
/// </summary>
[Collection("Sequential")]
public class MrSaturdayTeammateComparisonTests
{
    [Fact]
    public void BuildQualifyingGrid_Australia2026_ReturnsCorrectOrder()
    {
        // Arrange - Real data from 2026 Australian GP
        var qualifyingResults = GetAustralia2026QualifyingData();
        
        // Act
        var grid = ScoringService.BuildQualifyingGrid(qualifyingResults);
        
        // Assert - Verify grid positions match expected order
        grid.Should().HaveCount(19);
        grid[0].DriverId.Should().Be("russell");  // P1
        grid[0].GridPosition.Should().Be(1);
        grid[1].DriverId.Should().Be("antonelli");  // P2
        grid[1].GridPosition.Should().Be(2);
        grid[2].DriverId.Should().Be("hadjar");  // P3
        grid[3].DriverId.Should().Be("leclerc");  // P4
        grid[4].DriverId.Should().Be("piastri");  // P5
        grid[5].DriverId.Should().Be("norris");  // P6
        grid[6].DriverId.Should().Be("hamilton");  // P7
        grid[7].DriverId.Should().Be("lawson");  // P8
        grid[8].DriverId.Should().Be("arvid_lindblad");  // P9
        grid[9].DriverId.Should().Be("bortoleto");  // P10
        grid[18].DriverId.Should().Be("bottas");  // P19
    }
    
    [Fact]
    public void CompareTeammates_Ferrari_LeclercBeatsHamilton()
    {
        // Arrange - Ferrari teammates from Australia 2026
        var leclerc = new ScoringService.QualifyingGridPosition
        {
            DriverId = "leclerc",
            ConstructorId = "ferrari",
            Q1Time = ParseTime("1:20.226"),
            Q2Time = ParseTime("1:19.357"),
            Q3Time = ParseTime("1:19.327"),  // Faster Q3
            GridPosition = 4
        };
        
        var hamilton = new ScoringService.QualifyingGridPosition
        {
            DriverId = "hamilton",
            ConstructorId = "ferrari",
            Q1Time = ParseTime("1:19.811"),
            Q2Time = ParseTime("1:19.921"),
            Q3Time = ParseTime("1:19.478"),  // Slower Q3
            GridPosition = 7
        };
        
        // Act
        var result = ScoringService.CompareTeammates(leclerc, hamilton);
        
        // Assert - Leclerc wins (faster Q3 time)
        result.Should().Be(-1, "Leclerc should beat Hamilton");
    }
    
    [Fact]
    public void CompareTeammates_Mercedes_RussellBeatsAntonelli()
    {
        // Arrange
        var russell = new ScoringService.QualifyingGridPosition
        {
            DriverId = "russell",
            ConstructorId = "mercedes",
            Q3Time = ParseTime("1:18.518"),  // Faster
            GridPosition = 1
        };
        
        var antonelli = new ScoringService.QualifyingGridPosition
        {
            DriverId = "antonelli",
            ConstructorId = "mercedes",
            Q3Time = ParseTime("1:18.811"),  // Slower
            GridPosition = 2
        };
        
        // Act
        var result = ScoringService.CompareTeammates(russell, antonelli);
        
        // Assert
        result.Should().Be(-1, "Russell should beat Antonelli");
    }
    
    [Fact]
    public void CompareTeammates_SameQ3Time_FallsBackToQ2()
    {
        // Arrange - Synthetic scenario with tied Q3
        var driver1 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver1",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.000"),
            Q2Time = ParseTime("1:19.500"),  // Faster Q2
            Q3Time = ParseTime("1:19.000"),  // Same Q3
            GridPosition = 1
        };
        
        var driver2 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver2",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.100"),
            Q2Time = ParseTime("1:19.600"),  // Slower Q2
            Q3Time = ParseTime("1:19.000"),  // Same Q3
            GridPosition = 2
        };
        
        // Act
        var result = ScoringService.CompareTeammates(driver1, driver2);
        
        // Assert - Driver1 wins on Q2 tiebreaker
        result.Should().Be(-1, "Driver1 should win on Q2 tiebreaker");
    }
    
    [Fact]
    public void CompareTeammates_SameQ3AndQ2_FallsBackToQ1()
    {
        // Arrange
        var driver1 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver1",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.000"),  // Faster Q1
            Q2Time = ParseTime("1:19.500"),  // Same Q2
            Q3Time = ParseTime("1:19.000"),  // Same Q3
            GridPosition = 1
        };
        
        var driver2 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver2",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.100"),  // Slower Q1
            Q2Time = ParseTime("1:19.500"),  // Same Q2
            Q3Time = ParseTime("1:19.000"),  // Same Q3
            GridPosition = 2
        };
        
        // Act
        var result = ScoringService.CompareTeammates(driver1, driver2);
        
        // Assert
        result.Should().Be(-1, "Driver1 should win on Q1 tiebreaker");
    }
    
    [Fact]
    public void CompareTeammates_AllTimesSame_ReturnsTie()
    {
        // Arrange
        var driver1 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver1",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.000"),
            Q2Time = ParseTime("1:19.500"),
            Q3Time = ParseTime("1:19.000"),
            GridPosition = 1
        };
        
        var driver2 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver2",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.000"),  // Same
            Q2Time = ParseTime("1:19.500"),  // Same
            Q3Time = ParseTime("1:19.000"),  // Same
            GridPosition = 2
        };
        
        // Act
        var result = ScoringService.CompareTeammates(driver1, driver2);
        
        // Assert - Tie, no winner
        result.Should().Be(0, "Should be a tie when all times are equal");
    }
    
    [Fact]
    public void CompareTeammates_DifferentStages_HigherStageWins()
    {
        // Arrange - One driver in Q3, one eliminated in Q2
        var driverQ3 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver1",
            ConstructorId = "team",
            Q1Time = ParseTime("1:20.500"),
            Q2Time = ParseTime("1:20.000"),
            Q3Time = ParseTime("1:21.000"),  // Made Q3 even with slow time
            GridPosition = 10
        };
        
        var driverQ2 = new ScoringService.QualifyingGridPosition
        {
            DriverId = "driver2",
            ConstructorId = "team",
            Q1Time = ParseTime("1:19.000"),  // Faster Q1
            Q2Time = ParseTime("1:19.500"),  // Faster Q2
            Q3Time = null,  // Eliminated in Q2
            GridPosition = 11
        };
        
        // Act
        var result = ScoringService.CompareTeammates(driverQ3, driverQ2);
        
        // Assert - Driver who reached Q3 wins
        result.Should().Be(-1, "Driver who reached Q3 should beat driver eliminated in Q2");
    }
    
    [Fact]
    public void GetTeammateForDriver_FindsCorrectTeammate()
    {
        // Arrange
        var qualifyingResults = GetAustralia2026QualifyingData();
        var grid = ScoringService.BuildQualifyingGrid(qualifyingResults);
        
        // Act - Find Hamilton's teammate (Leclerc, both Ferrari)
        var teammate = ScoringService.GetTeammateForDriver("hamilton", "ferrari", grid);
        
        // Assert
        teammate.Should().NotBeNull();
        teammate!.DriverId.Should().Be("leclerc");
        teammate.ConstructorId.Should().Be("ferrari");
    }
    
    [Fact]
    public void GetTeammateForDriver_NoTeammate_ReturnsNull()
    {
        // Arrange - Single driver grid (edge case)
        var grid = new List<ScoringService.QualifyingGridPosition>
        {
            new ScoringService.QualifyingGridPosition
            {
                DriverId = "lonely_driver",
                ConstructorId = "lonely_team",
                Q1Time = ParseTime("1:20.000"),
                GridPosition = 1
            }
        };
        
        // Act
        var teammate = ScoringService.GetTeammateForDriver("lonely_driver", "lonely_team", grid);
        
        // Assert
        teammate.Should().BeNull("No teammate exists for this driver");
    }
    
    // Helper method to get real Australia 2026 qualifying data
    private static List<Qualifying> GetAustralia2026QualifyingData()
    {
        return new List<Qualifying>
        {
            new Qualifying { Season = "2026", Round = "1", Position = "1", DriverId = "russell", ConstructorId = "mercedes", Number = "63", Q1 = "1:19.507", Q2 = "1:18.934", Q3 = "1:18.518" },
            new Qualifying { Season = "2026", Round = "1", Position = "2", DriverId = "antonelli", ConstructorId = "mercedes", Number = "12", Q1 = "1:20.120", Q2 = "1:19.435", Q3 = "1:18.811" },
            new Qualifying { Season = "2026", Round = "1", Position = "3", DriverId = "hadjar", ConstructorId = "red_bull", Number = "6", Q1 = "1:20.023", Q2 = "1:19.653", Q3 = "1:19.303" },
            new Qualifying { Season = "2026", Round = "1", Position = "4", DriverId = "leclerc", ConstructorId = "ferrari", Number = "16", Q1 = "1:20.226", Q2 = "1:19.357", Q3 = "1:19.327" },
            new Qualifying { Season = "2026", Round = "1", Position = "5", DriverId = "piastri", ConstructorId = "mclaren", Number = "81", Q1 = "1:19.664", Q2 = "1:19.525", Q3 = "1:19.380" },
            new Qualifying { Season = "2026", Round = "1", Position = "6", DriverId = "norris", ConstructorId = "mclaren", Number = "1", Q1 = "1:20.010", Q2 = "1:19.882", Q3 = "1:19.475" },
            new Qualifying { Season = "2026", Round = "1", Position = "7", DriverId = "hamilton", ConstructorId = "ferrari", Number = "44", Q1 = "1:19.811", Q2 = "1:19.921", Q3 = "1:19.478" },
            new Qualifying { Season = "2026", Round = "1", Position = "8", DriverId = "lawson", ConstructorId = "rb", Number = "30", Q1 = "1:20.491", Q2 = "1:20.144", Q3 = "1:19.994" },
            new Qualifying { Season = "2026", Round = "1", Position = "9", DriverId = "arvid_lindblad", ConstructorId = "rb", Number = "41", Q1 = "1:20.409", Q2 = "1:19.971", Q3 = "1:21.247" },
            new Qualifying { Season = "2026", Round = "1", Position = "10", DriverId = "bortoleto", ConstructorId = "audi", Number = "5", Q1 = "1:20.495", Q2 = "1:20.221", Q3 = "" },
            new Qualifying { Season = "2026", Round = "1", Position = "11", DriverId = "hulkenberg", ConstructorId = "audi", Number = "27", Q1 = "1:21.024", Q2 = "1:20.303", Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "12", DriverId = "bearman", ConstructorId = "haas", Number = "87", Q1 = "1:21.247", Q2 = "1:20.311", Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "13", DriverId = "ocon", ConstructorId = "haas", Number = "31", Q1 = "1:20.759", Q2 = "1:20.491", Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "14", DriverId = "gasly", ConstructorId = "alpine", Number = "10", Q1 = "1:21.138", Q2 = "1:20.501", Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "15", DriverId = "albon", ConstructorId = "williams", Number = "23", Q1 = "1:21.051", Q2 = "1:20.941", Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "16", DriverId = "colapinto", ConstructorId = "alpine", Number = "43", Q1 = "1:21.200", Q2 = "1:21.270", Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "17", DriverId = "alonso", ConstructorId = "aston_martin", Number = "14", Q1 = "1:21.969", Q2 = null, Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "18", DriverId = "perez", ConstructorId = "cadillac", Number = "11", Q1 = "1:22.605", Q2 = null, Q3 = null },
            new Qualifying { Season = "2026", Round = "1", Position = "19", DriverId = "bottas", ConstructorId = "cadillac", Number = "77", Q1 = "1:23.244", Q2 = null, Q3 = null }
        };
    }
    
    // Helper to parse lap time strings
    private static TimeSpan? ParseTime(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr))
            return null;
        
        var parts = timeStr.Split(':');
        if (parts.Length != 2)
            return null;
        
        var minutes = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var seconds = double.Parse(parts[1], CultureInfo.InvariantCulture);
        
        return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
    }
}
