using GDG_DashBoard.DAL.Models;

namespace GDG_DashBoard.BLL.Services.Group;

public interface IGroupService
{
    Task<CommunityGroup> CreateGroupAsync(string name, string description, Guid instructorId);
    Task AddMembersToGroupBulkAsync(Guid groupId, List<Guid> memberIds);
    Task AssignRoadmapToGroupAsync(Guid groupId, Guid roadmapId);
    Task<List<CommunityGroup>> GetGroupsForInstructorAsync(Guid instructorId);
    Task<CommunityGroup?> GetGroupDetailsAsync(Guid groupId);
    Task<List<ApplicationUser>> GetAvailableUsersAsync();
    Task<List<Roadmap>> GetAvailableRoadmapsAsync();
    
    Task<bool> JoinGroupByCodeAsync(Guid userId, string joinCode);
}
