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

    public async Task<List<CommunityGroup>> GetAllGroupsAsync()
    {
        return await _context.CommunityGroups
            .Include(g => g.Roadmap)
            .Include(g => g.GroupMembers)
            .ToListAsync();
    }

    public async Task<List<CommunityGroup>> GetOpenCohortsAsync(int count = 8)
    {
        return await _context.CommunityGroups
            .Include(g => g.Roadmap)
            .Include(g => g.GroupMembers)
            .OrderByDescending(g => g.CreatedAt)
            .Take(count)
            .ToListAsync();
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
        if (!newMemberIds.Any()) return;

        var newMembers = newMemberIds.Select(id => new GroupMember
        {
            GroupId = groupId,
            MemberId = id
        });

        _context.GroupMembers.AddRange(newMembers);
        await _context.SaveChangesAsync();

        // Auto-enrollment removed. Users must manually opt-in.
    }

    public async Task AssignRoadmapToGroupAsync(Guid groupId, Guid roadmapId)
    {
        var group = await _context.CommunityGroups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new Exception("Group not found");

        group.RoadmapId = roadmapId;
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

        await _context.SaveChangesAsync();
        return true;
    }
}
