using GDG_DashBoard.BLL.Services.Role;
using GDG_DashBoard.BLL.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GDG_DashBoard.Controllers;

[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Route("Role/Manage/{id:guid}")]
    public async Task<IActionResult> ManageRoles(Guid id, string? returnUrl = null)
    {
        var vm = await _roleService.GetManageRolesViewModelAsync(id);
        if (vm is null)
            return NotFound();

        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Admin/ManageRoles.cshtml", vm);
    }

    [HttpPost]
    [Route("Role/Manage/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRoles(Guid id, ManageUserRolesViewModel model, string? returnUrl = null)
    {
        var success = await _roleService.UpdateUserRolesAsync(id, model.Roles);

        if (success)
            TempData["SuccessMessage"] = "Roles updated successfully.";
        else
            TempData["ErrorMessage"] = "Failed to update roles.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Admin");
    }
}
