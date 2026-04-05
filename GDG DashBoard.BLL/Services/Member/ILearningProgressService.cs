using GDG_DashBoard.BLL.Dtos.Member;

namespace GDG_DashBoard.BLL.Services.Member;

public interface ILearningProgressService
{
    Task EnrollInRoadmapAsync(Guid userId, Guid roadmapId);
    Task<RoadmapDetailsForMemberDto?> GetRoadmapDetailsForMemberAsync(Guid roadmapId, Guid userId);
    Task<ToggleProgressResultDto> ToggleNodeProgressAsync(Guid userId, Guid levelId);
    Task<ToggleResourceResultDto> ToggleResourceProgressAsync(Guid userId, Guid resourceId);
    /// <summary>Called after a quiz is passed — finds the matching Quiz resource and marks it complete.</summary>
    Task SyncQuizResourceProgressAsync(Guid userId, Guid quizId);

    Task LogQuizViolationAsync(Guid userId, Guid quizId, string violationType);
}
