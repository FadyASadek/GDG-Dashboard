using GDG_DashBoard.BLL.Dtos.Member;
using GDG_DashBoard.BLL.ViewModels.Member;

namespace GDG_DashBoard.BLL.Services.Member;

public interface IMemberService
{
    Task<MemberDashboardDto?> GetMemberDashboardAsync(Guid userId);
    Task<MemberProfileViewModel> GetProfileForEditAsync(Guid userId);
    Task UpdateMemberProfileAsync(Guid userId, MemberProfileViewModel model);
    Task<RoadmapDetailsForMemberDto?> GetRoadmapDetailsForMemberAsync(Guid roadmapId, Guid userId);
    Task<ToggleProgressResultDto> ToggleNodeProgressAsync(Guid userId, Guid levelId);
    Task<ToggleResourceResultDto> ToggleResourceProgressAsync(Guid userId, Guid resourceId);
    /// <summary>Called after a quiz is passed — finds the matching Quiz resource and marks it complete.</summary>
    Task SyncQuizResourceProgressAsync(Guid userId, Guid quizId);
}
