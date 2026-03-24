using GDG_DashBoard.BLL.ViewModels.Admin;
using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.BLL.Services.Role;

public class RoleService : IRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RoleService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ManageUserRolesViewModel?> GetManageRolesViewModelAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        var userRoles = await _userManager.GetRolesAsync(user);

        var vm = new ManageUserRolesViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? user.Email ?? "Unknown",
            Roles = allRoles.Select(role => new RoleSelection
            {
                RoleName = role,
                IsSelected = userRoles.Contains(role)
            }).ToList()
        };

        return vm;
    }

    public async Task<bool> UpdateUserRolesAsync(Guid userId, List<RoleSelection> model)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        var currentRoles = await _userManager.GetRolesAsync(user);

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded) return false;

        var selectedRoles = model.Where(x => x.IsSelected).Select(x => x.RoleName).ToList();
        var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);
        
        return addResult.Succeeded;
    }
}
