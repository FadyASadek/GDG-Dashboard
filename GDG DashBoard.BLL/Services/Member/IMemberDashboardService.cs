using GDG_DashBoard.BLL.Dtos.Member;

namespace GDG_DashBoard.BLL.Services.Member;

public interface IMemberDashboardService
{
    Task<MemberDashboardDto?> GetMemberDashboardAsync(Guid userId);
}
