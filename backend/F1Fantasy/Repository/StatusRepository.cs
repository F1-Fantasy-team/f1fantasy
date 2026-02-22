using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class StatusRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<StatusRepository> _logger;

    public StatusRepository(F1FantasyDbContext context, ILogger<StatusRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(Status status)
    {
        var existingStatus = await _context.Statuses
            .FirstOrDefaultAsync(s => s.StatusId == status.StatusId);

        if (existingStatus != null)
        {
            // Update the status text and count
            existingStatus.StatusText = status.StatusText;
            existingStatus.Count = status.Count;
            
            _logger.LogDebug("Updating status: {StatusId} - {StatusText}", status.StatusId, status.StatusText);
        }
        else
        {
            await _context.Statuses.AddAsync(status);
            _logger.LogDebug("Adding new status: {StatusId} - {StatusText}", status.StatusId, status.StatusText);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Status?> GetByIdAsync(string statusId)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(s => s.StatusId == statusId);
    }

    public async Task<Status?> GetByTextAsync(string statusText)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(s => s.StatusText == statusText);
    }

    public async Task<List<Status>> GetAllAsync()
    {
        var statuses = await _context.Statuses.ToListAsync();
        
        // Sort by count (most common first), then by status text
        return statuses
            .OrderByDescending(s => int.TryParse(s.Count, out var count) ? count : 0)
            .ThenBy(s => s.StatusText)
            .ToList();
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all statuses from database");
        _context.Statuses.RemoveRange(_context.Statuses);
        await _context.SaveChangesAsync();
    }
}
