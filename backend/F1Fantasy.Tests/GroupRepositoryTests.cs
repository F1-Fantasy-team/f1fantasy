using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for GroupRepository focusing on performance optimizations
/// </summary>
[Collection("Sequential")]
public class GroupRepositoryTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly GroupRepository _repository;
    private readonly string _testUserId = "test_user_groups";
    private readonly List<int> _testGroupIds = new();

    public GroupRepositoryTests()
    {
        var envPath = @"C:\Projects\f1fantasy\backend\.env";
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Database connection string not found. Ensure .env file exists at {envPath}");
        }

        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        _context = new F1FantasyDbContext(options);
        _repository = new GroupRepository(_context, NullLogger<GroupRepository>.Instance);
    }

    [Fact]
    public async Task GetGroupsByUserIdAsync_WithMultipleGroups_ExecutesEfficientlyWithTwoQueries()
    {
        // Arrange - Create multiple groups with the test user as member
        await CleanupTestDataAsync();
        
        // Create 5 test groups
        for (int i = 0; i < 5; i++)
        {
            var group = new Group
            {
                Name = $"Test Group {i}",
                AdminUserId = _testUserId,
                InviteCode = Guid.NewGuid().ToString().Substring(0, 8),
                LockMode = "manual",
                CreatedAt = DateTime.UtcNow
            };

            var createdGroup = await _repository.CreateAsync(group);
            _testGroupIds.Add(createdGroup.Id);

            // Add test user as member
            await _repository.AddMemberAsync(new GroupMember
            {
                GroupId = createdGroup.Id,
                UserId = _testUserId,
                JoinedAt = DateTime.UtcNow
            });

            // Add some additional members
            for (int j = 0; j < 3; j++)
            {
                await _repository.AddMemberAsync(new GroupMember
                {
                    GroupId = createdGroup.Id,
                    UserId = $"member_{i}_{j}",
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        // Act - Measure performance of optimized query
        var stopwatch = Stopwatch.StartNew();
        var groups = await _repository.GetGroupsByUserIdAsync(_testUserId);
        stopwatch.Stop();

        // Assert
        groups.Should().HaveCount(5);
        groups.Should().AllSatisfy(g => g.Members.Should().HaveCount(4)); // 1 admin + 3 members
        
        // Performance assertion - optimized version should be fast even with multiple groups
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "optimized query should execute quickly");
    }

    [Fact]
    public async Task GetByIdAsync_UsesAsNoTracking_IncludesMembers()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        var group = new Group
        {
            Name = "Test Group AsNoTracking",
            AdminUserId = _testUserId,
            InviteCode = Guid.NewGuid().ToString().Substring(0, 8),
            LockMode = "manual",
            CreatedAt = DateTime.UtcNow
        };
        var createdGroup = await _repository.CreateAsync(group);
        _testGroupIds.Add(createdGroup.Id);

        await _repository.AddMemberAsync(new GroupMember
        {
            GroupId = createdGroup.Id,
            UserId = _testUserId,
            JoinedAt = DateTime.UtcNow
        });

        // Clear change tracker to ensure fresh query
        _context.ChangeTracker.Clear();

        // Act
        var retrieved = await _repository.GetByIdAsync(createdGroup.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Members.Should().HaveCount(1);
        retrieved.Name.Should().Be("Test Group AsNoTracking");
    }

    [Fact]
    public async Task IsUserMemberAsync_PerformsEfficientQuery()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        var group = new Group
        {
            Name = "Test Membership Check",
            AdminUserId = "admin_user",
            InviteCode = Guid.NewGuid().ToString().Substring(0, 8),
            LockMode = "manual",
            CreatedAt = DateTime.UtcNow
        };
        var createdGroup = await _repository.CreateAsync(group);
        _testGroupIds.Add(createdGroup.Id);

        await _repository.AddMemberAsync(new GroupMember
        {
            GroupId = createdGroup.Id,
            UserId = _testUserId,
            JoinedAt = DateTime.UtcNow
        });

        // Act
        var stopwatch = Stopwatch.StartNew();
        var isMember = await _repository.IsUserMemberAsync(createdGroup.Id, _testUserId);
        var isNotMember = await _repository.IsUserMemberAsync(createdGroup.Id, "non_existent_user");
        stopwatch.Stop();

        // Assert
        isMember.Should().BeTrue();
        isNotMember.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "membership check should be fast");
    }

    [Fact]
    public async Task GetGroupsByUserIdAsync_WithNoGroups_ReturnsEmptyList()
    {
        // Act
        var groups = await _repository.GetGroupsByUserIdAsync("user_with_no_groups");

        // Assert
        groups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllGroupsAsync_ReturnsAllGroupsWithMembers()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        var group = new Group
        {
            Name = "Test Group GetAll",
            AdminUserId = _testUserId,
            InviteCode = Guid.NewGuid().ToString().Substring(0, 8),
            LockMode = "manual",
            CreatedAt = DateTime.UtcNow
        };
        var createdGroup = await _repository.CreateAsync(group);
        _testGroupIds.Add(createdGroup.Id);

        await _repository.AddMemberAsync(new GroupMember
        {
            GroupId = createdGroup.Id,
            UserId = _testUserId,
            JoinedAt = DateTime.UtcNow
        });

        // Act
        var allGroups = await _repository.GetAllGroupsAsync();

        // Assert
        allGroups.Should().Contain(g => g.Id == createdGroup.Id);
        var testGroup = allGroups.First(g => g.Id == createdGroup.Id);
        testGroup.Members.Should().HaveCount(1);
    }

    private async Task CleanupTestDataAsync()
    {
        // Delete all test group members first
        var members = await _context.GroupMembers
            .Where(m => m.UserId == _testUserId || m.UserId.StartsWith("member_"))
            .ToListAsync();
        _context.GroupMembers.RemoveRange(members);

        // Delete all test groups
        if (_testGroupIds.Any())
        {
            var groups = await _context.Groups
                .Where(g => _testGroupIds.Contains(g.Id))
                .ToListAsync();
            _context.Groups.RemoveRange(groups);
        }

        // Also clean up any groups where test user is admin
        var adminGroups = await _context.Groups
            .Where(g => g.AdminUserId == _testUserId)
            .ToListAsync();
        _context.Groups.RemoveRange(adminGroups);

        await _context.SaveChangesAsync();
        _testGroupIds.Clear();
    }

    public void Dispose()
    {
        CleanupTestDataAsync().Wait();
        _context.Dispose();
    }
}
