using GDG_DashBoard.BLL.Dtos.Admin;
using GDG_DashBoard.BLL.Dtos.Auth;

namespace GDG_DashBoard.BLL.Services.Admin;

public interface IAdminService
{
    Task<DashboardOverviewDto> GetDashboardOverviewAsync();
    Task<List<MemberOverviewDto>> GetAllMembersAsync(string? searchString);
    Task<bool> ResendActivationTokenAsync(Guid userId);
    Task<MemberDetailsDto?> GetMemberDetailsAsync(Guid userId);
    Task<AuthResultDto> OnboardMemberAsync(OnboardMemberDto dto);
}
