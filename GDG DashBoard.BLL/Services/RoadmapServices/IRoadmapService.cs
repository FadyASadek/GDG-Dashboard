using GDG_DashBoard.DAL.Models;
using GDG_DashBoard.DAL.Eums;

namespace GDG_DashBoard.BLL.Services.RoadmapServices;

public interface IRoadmapService
{
    Task<List<Roadmap>> GetAllRoadmapsAsync();
    Task<List<Roadmap>> GetRoadmapsByCreatorAsync(Guid userId);
    Task<List<Roadmap>> GetPublicRoadmapsAsync(int count = 12);
    Task<Roadmap?> GetRoadmapDetailsAsync(Guid roadmapId);
    Task<RoadmapLevel> AddLevelAsync(Guid roadmapId, string title, string? instructions, int orderIndex, Guid? quizId = null);
    Task<Resource> AddResourceAsync(Guid levelId, string title, string url, string? thumbnailUrl, int estimatedMinutes, int orderIndex, ResourceType type);
    Task<bool> DeleteLevelAsync(Guid levelId);
    Task<bool> DeleteResourceAsync(Guid resourceId);
}
