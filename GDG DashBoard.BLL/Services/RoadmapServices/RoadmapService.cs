using GDG_DashBoard.DAL.Models;
using GDG_DashBoard.DAL.Eums;
using GDGDashBoard.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.BLL.Services.RoadmapServices;

public class RoadmapService : IRoadmapService
{
    private readonly AppDbContext _context;

    public RoadmapService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Roadmap?> GetRoadmapDetailsAsync(Guid roadmapId)
    {
        return await _context.Roadmaps
            .Include(r => r.CreatedBy)
            .Include(r => r.Levels.OrderBy(l => l.OrderIndex))
                .ThenInclude(l => l.Resources.OrderBy(res => res.OrderIndex))
            .FirstOrDefaultAsync(r => r.Id == roadmapId);
    }

    public async Task<RoadmapLevel> AddLevelAsync(Guid roadmapId, string title, string? instructions, int orderIndex)
    {
        var level = new RoadmapLevel
        {
            RoadmapId = roadmapId,
            Title = title,
            Instructions = instructions,
            OrderIndex = orderIndex
        };

        _context.RoadmapLevels.Add(level);
        await _context.SaveChangesAsync(); 

        // CRITICAL SYNC: 
        // We must find any user already enrolled in this Roadmap and add a UserNodeProgress for this new level!
        var enrolledUsers = await _context.UserEnrollments
            .Where(e => e.RoadmapId == roadmapId)
            .Select(e => e.UserId)
            .ToListAsync();

        if (enrolledUsers.Any())
        {
            var newProgresses = enrolledUsers.Select(userId => new UserNodeProgress
            {
                UserId = userId,
                RoadmapLevelId = level.Id,
                IsCompleted = false
            });
            _context.UserNodeProgresses.AddRange(newProgresses);
            await _context.SaveChangesAsync();
        }

        return level;
    }

    public async Task<Resource> AddResourceAsync(Guid levelId, string title, string url, string? thumbnailUrl, int estimatedMinutes, int orderIndex, ResourceType type)
    {
        var resource = new Resource
        {
            RoadmapLevelId = levelId,
            Title = title,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            EstimatedMinutes = estimatedMinutes,
            OrderIndex = orderIndex,
            Type = type
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        return resource;
    }

    public async Task<bool> DeleteLevelAsync(Guid levelId)
    {
        var level = await _context.RoadmapLevels.FindAsync(levelId);
        if (level == null) return false;

        _context.RoadmapLevels.Remove(level);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteResourceAsync(Guid resourceId)
    {
        var resource = await _context.Resources.FindAsync(resourceId);
        if (resource == null) return false;

        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();
        return true;
    }
}
