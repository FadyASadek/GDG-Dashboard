using GDG_DashBoard.BLL.ViewModels.Member;

namespace GDG_DashBoard.BLL.Services.Member;

public interface IMemberProfileService
{
    Task<MemberProfileViewModel> GetProfileForEditAsync(Guid userId);
    Task UpdateMemberProfileAsync(Guid userId, MemberProfileViewModel model);
}
