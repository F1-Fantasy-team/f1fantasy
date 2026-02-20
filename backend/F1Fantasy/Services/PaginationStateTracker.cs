using System.Collections.Concurrent;

namespace F1Fantasy.Services;

/// <summary>
/// Tracks pagination state for API endpoints to resume from last successful offset
/// when requests fail due to rate limiting or other errors
/// </summary>
public class PaginationStateTracker
{
    private readonly ConcurrentDictionary<string, PaginationState> _states = new();

    public class PaginationState
    {
        public int LastSuccessfulOffset { get; set; } = 0;
        public int Total { get; set; } = 0;
        public bool IsComplete { get; set; } = false;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the current pagination state for an endpoint
    /// </summary>
    public PaginationState GetState(string endpoint)
    {
        return _states.GetOrAdd(endpoint, _ => new PaginationState());
    }

    /// <summary>
    /// Update the pagination state after a successful fetch
    /// </summary>
    public void UpdateState(string endpoint, int offset, int total)
    {
        var state = _states.GetOrAdd(endpoint, _ => new PaginationState());
        state.LastSuccessfulOffset = offset;
        state.Total = total;
        state.LastUpdate = DateTime.UtcNow;
        
        // Mark as complete if we've reached or exceeded the total
        const int limit = 30; // API page size
        state.IsComplete = (offset + limit) >= total;
    }

    /// <summary>
    /// Mark pagination as complete for an endpoint
    /// </summary>
    public void MarkComplete(string endpoint)
    {
        var state = _states.GetOrAdd(endpoint, _ => new PaginationState());
        state.IsComplete = true;
        state.LastUpdate = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset pagination state for an endpoint (force refresh)
    /// </summary>
    public void Reset(string endpoint)
    {
        _states.TryRemove(endpoint, out _);
    }

    /// <summary>
    /// Check if we should attempt to fetch more data
    /// Returns true if incomplete or data is stale (older than 1 hour)
    /// </summary>
    public bool ShouldFetch(string endpoint)
    {
        if (!_states.TryGetValue(endpoint, out var state))
        {
            return true; // No state, should fetch
        }

        if (!state.IsComplete)
        {
            return true; // Incomplete, continue fetching
        }

        // If complete but data is older than 1 hour, refresh
        var dataAge = DateTime.UtcNow - state.LastUpdate;
        return dataAge.TotalHours > 1;
    }

    /// <summary>
    /// Get the next offset to fetch from
    /// </summary>
    public int GetNextOffset(string endpoint)
    {
        if (!_states.TryGetValue(endpoint, out var state))
        {
            return 0; // Start from beginning
        }

        if (state.IsComplete)
        {
            return 0; // Restart if complete and doing a refresh
        }

        // Continue from after the last successful offset
        const int limit = 30;
        return state.LastSuccessfulOffset + limit;
    }
}
