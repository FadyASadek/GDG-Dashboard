using GDG_DashBoard.BLL.ViewModels.Admin;

namespace GDG_DashBoard.BLL.Services.Role;

public interface IRoleService
{
    Task<ManageUserRolesViewModel?> GetManageRolesViewModelAsync(Guid userId);
    Task<bool> UpdateUserRolesAsync(Guid userId, List<RoleSelection> model);
}
