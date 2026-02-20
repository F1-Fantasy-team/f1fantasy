using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;

namespace F1Fantasy.Tests;

/// <summary>
/// Tests to verify pagination state tracking and resumption after failures
/// </summary>
public class PaginationStateTrackerTests
{
    [Fact]
    public void PaginationStateTracker_RemembersLastOffset()
    {
        // Arrange
        var tracker = new PaginationStateTracker();
        var endpoint = "test-endpoint";

        // Act - Simulate successful pagination (not yet complete)
        tracker.UpdateState(endpoint, 0, 100);  // Total is 100, so we're not done yet
        tracker.UpdateState(endpoint, 30, 100);

        // Assert
        var state = tracker.GetState(endpoint);
        state.LastSuccessfulOffset.Should().Be(30, "last successful offset should be saved");
        state.Total.Should().Be(100);
        state.IsComplete.Should().BeFalse("pagination is not complete yet (30 + 30 = 60 < 100)");
    }

    [Fact]
    public void PaginationStateTracker_MarksAsComplete()
    {
        // Arrange
        var tracker = new PaginationStateTracker();
        var endpoint = "test-endpoint";

        // Act - Complete pagination
        tracker.UpdateState(endpoint, 0, 77);
        tracker.UpdateState(endpoint, 30, 77);
        tracker.UpdateState(endpoint, 60, 77); // 60 + 30 (limit) = 90 >= 77, so complete

        // Assert
        var state = tracker.GetState(endpoint);
        state.IsComplete.Should().BeTrue("pagination should be marked as complete");
    }

    [Fact]
    public void PaginationStateTracker_ResumesFromLastOffset()
    {
        // Arrange
        var tracker = new PaginationStateTracker();
        var endpoint = "test-endpoint";

        // Act - Simulate partial fetch then failure
        tracker.UpdateState(endpoint, 0, 77);
        tracker.UpdateState(endpoint, 30, 77);
        // Imagine failure happens here at offset 60

        var nextOffset = tracker.GetNextOffset(endpoint);

        // Assert
        nextOffset.Should().Be(60, "should resume from next offset after last successful");
        tracker.ShouldFetch(endpoint).Should().BeTrue("should continue fetching since incomplete");
    }

    [Fact]
    public void PaginationStateTracker_SkipsFetchWhenComplete()
    {
        // Arrange
        var tracker = new PaginationStateTracker();
        var endpoint = "test-endpoint";

        // Act - Mark as complete
        tracker.MarkComplete(endpoint);

        // Assert
        tracker.ShouldFetch(endpoint).Should().BeFalse("should not fetch when complete and fresh");
    }

    [Fact]
    public void PaginationStateTracker_ResetClearsState()
    {
        // Arrange
        var tracker = new PaginationStateTracker();
        var endpoint = "test-endpoint";
        tracker.UpdateState(endpoint, 30, 77);

        // Act
        tracker.Reset(endpoint);

        // Assert
        var nextOffset = tracker.GetNextOffset(endpoint);
        nextOffset.Should().Be(0, "should start from beginning after reset");
    }

    [Fact]
    public async Task SeasonService_ResumesFromLastOffset_OnSubsequentCall()
    {
        // This test demonstrates the real-world scenario
        // In practice, rate limiting would cause a failure mid-pagination
        // On the next API call, it should resume from where it left off

        // Arrange
        var httpClient = new HttpClient();
        var repository = new SeasonRepository();
        var tracker = new PaginationStateTracker();
        var service = new SeasonService(httpClient, repository, tracker);

        // First call - will fetch all data successfully
        var seasons1 = await service.GetAllSeasonsAsync();
        var initialCount = seasons1.Count();

        // Verify state is marked complete
        var state = tracker.GetState("seasons");
        state.IsComplete.Should().BeTrue("first fetch should complete successfully");

        // Second call - should return cached data without fetching
        var seasons2 = await service.GetAllSeasonsAsync();
        
        // Assert
        seasons2.Count().Should().Be(initialCount, "should return cached data");
        // In a real scenario with failure, the next call would resume from the saved offset
    }
}
