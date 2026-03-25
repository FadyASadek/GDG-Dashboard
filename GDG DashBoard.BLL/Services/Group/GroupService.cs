using GDG_DashBoard.DAL.Models;
using GDGDashBoard.DAL.Data;
using Microsoft.EntityFrameworkCore;
using GDG_DashBoard.DAL.Eums;
namespace GDG_DashBoard.BLL.Services.Group;

public class GroupService : IGroupService
{
    private readonly AppDbContext _context;

    public GroupService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CommunityGroup> CreateGroupAsync(string name, string description, Guid instructorId)
    {
        var group = new CommunityGroup
        {
            Name = name,
            Description = description,
            InstructorId = instructorId
        };
        
        _context.CommunityGroups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<List<ApplicationUser>> GetAvailableUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<List<Roadmap>> GetAvailableRoadmapsAsync()
    {
        return await _context.Roadmaps.ToListAsync();
    }

    public async Task<List<CommunityGroup>> GetGroupsForInstructorAsync(Guid instructorId)
    {
        return await _context.CommunityGroups
            .Include(g => g.Roadmap)
            .Include(g => g.GroupMembers)
            .Where(g => g.InstructorId == instructorId)
            .ToListAsync();
    }

    public async Task<CommunityGroup?> GetGroupDetailsAsync(Guid groupId)
    {
        return await _context.CommunityGroups
            .Include(g => g.Roadmap)
            .Include(g => g.GroupMembers)
                .ThenInclude(gm => gm.Member)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task AddMembersToGroupBulkAsync(Guid groupId, List<Guid> memberIds)
    {
        var existingMembers = await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Select(gm => gm.MemberId)
            .ToListAsync();

        var newMemberIds = memberIds.Except(existingMembers).ToList();

        var newMembers = newMemberIds.Select(id => new GroupMember
        {
            GroupId = groupId,
            MemberId = id
        });

        _context.GroupMembers.AddRange(newMembers);
        await _context.SaveChangesAsync();
    }

    public async Task AssignRoadmapToGroupAsync(Guid groupId, Guid roadmapId)
    {
        var group = await _context.CommunityGroups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception("Group not found");

        group.RoadmapId = roadmapId;

        var memberIds = group.GroupMembers.Select(gm => gm.MemberId).ToList();

        var roadmap = await _context.Roadmaps
            .Include(r => r.Levels)
            .FirstOrDefaultAsync(r => r.Id == roadmapId);

        if (roadmap == null) throw new Exception("Roadmap not found");

        var existingEnrollments = await _context.UserEnrollments
            .Where(e => e.RoadmapId == roadmapId && memberIds.Contains(e.UserId))
            .ToListAsync();

        var existingEnrolledUserIds = existingEnrollments.Select(e => e.UserId).ToHashSet();
        
        var newEnrollments = new List<UserEnrollment>();
        var newProgresses = new List<UserNodeProgress>();

        foreach (var memberId in memberIds)
        {
            if (!existingEnrolledUserIds.Contains(memberId))
            {
                newEnrollments.Add(new UserEnrollment
                {
                    UserId = memberId,
                    RoadmapId = roadmapId,
                    Status = EnrollmentStatus.InProgress,
                    ProgressPercentage = 0
                });

                foreach (var level in roadmap.Levels)
                {
                    newProgresses.Add(new UserNodeProgress
                    {
                        UserId = memberId,
                        RoadmapLevelId = level.Id,
                        IsCompleted = false
                    });
                }
            }
        }

        if (newEnrollments.Any()) _context.UserEnrollments.AddRange(newEnrollments);
        if (newProgresses.Any()) _context.UserNodeProgresses.AddRange(newProgresses);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> JoinGroupByCodeAsync(Guid userId, string joinCode)
    {
        var group = await _context.CommunityGroups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.JoinCode == joinCode);

        if (group == null) throw new Exception("Invalid Join Code");

        if (group.GroupMembers.Any(gm => gm.MemberId == userId))
            return true; // Already joined

        _context.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            MemberId = userId
        });

        if (group.RoadmapId.HasValue)
        {
            var isEnrolled = await _context.UserEnrollments
                .AnyAsync(e => e.UserId == userId && e.RoadmapId == group.RoadmapId.Value);

            if (!isEnrolled)
            {
                var roadmap = await _context.Roadmaps.Include(r => r.Levels).FirstAsync(r => r.Id == group.RoadmapId.Value);
                _context.UserEnrollments.Add(new UserEnrollment
                {
                    UserId = userId,
                    RoadmapId = roadmap.Id,
                    Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.InProgress,
                    ProgressPercentage = 0
                });

                foreach(var level in roadmap.Levels)
                {
                    _context.UserNodeProgresses.Add(new UserNodeProgress
                    {
                        UserId = userId,
                        RoadmapLevelId = level.Id,
                        IsCompleted = false
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
