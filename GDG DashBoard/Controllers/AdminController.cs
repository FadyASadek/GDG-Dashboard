using GDG_DashBoard.BLL.Dtos.Admin;
using GDG_DashBoard.BLL.Services.Admin;
using GDG_DashBoard.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.Controllers;

[Authorize(Roles = "Admin")]

public class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var overview = await _adminService.GetDashboardOverviewAsync();

        var vm = new DashboardViewModel
        {
            TotalMembers = overview.TotalMembers,
            ActiveThisWeek = overview.ActiveThisWeek,
            TotalRoadmaps = overview.TotalRoadmaps,
            PendingReviewRoadmaps = overview.PendingReviewRoadmaps,
            Members = overview.Members,
            TopContributors = overview.TopContributors
        };

        return View(vm);
    }

    [HttpGet]
    [Route("Admin/Member/{id:guid}")]
    public async Task<IActionResult> Member(Guid id)
    {
        var details = await _adminService.GetMemberDetailsAsync(id);
        if (details is null)
            return NotFound();

        return View(new MemberDetailViewModel { Member = details });
    }

    [HttpGet]
    [Route("Admin/Roadmap/{id:guid}")]
    public async Task<IActionResult> Roadmap(Guid id)
    {
        var details = await _adminService.GetMemberDetailsAsync(id);
        if (details is null)
            return NotFound();

        var vm = new MemberDetailViewModel { Member = details };

        return View(vm);
    }

  
    [HttpGet]
    public IActionResult Onboard() => View(new OnboardViewModel());

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onboard(OnboardViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var dto = new OnboardMemberDto
        {
            Email = model.Email,
            FullName = model.FullName,
            Season = model.Season,
            Role = model.Role
        };

        var result = await _adminService.OnboardMemberAsync(dto);

        if (result.Succeeded)
        {
            model.OnboardingSucceeded = true;
            model.GeneratedPasswordResetToken = result.PasswordResetToken;
            TempData["SuccessMessage"] = $"Member '{model.FullName}' onboarded successfully.";
            return View(model);  // Stay on page to show the reset token to the admin
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Onboarding failed.");
        return View(model);
    }

    [HttpGet]
    [Route("Admin/AllMembers")]
    public async Task<IActionResult> AllMembers(string? searchString)
    {
        ViewData["SearchString"] = searchString;
        var members = await _adminService.GetAllMembersAsync(searchString);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_MembersTablePartial", members);
        }

        return View(members);
    }

    [HttpPost]
    [Route("Admin/ResendActivationToken/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendActivationToken(Guid id)
    {
        try
        {
            var success = await _adminService.ResendActivationTokenAsync(id);
            if (success)
                TempData["SuccessMessage"] = "Setup link sent successfully!";
            else
                TempData["ErrorMessage"] = "User not found.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Email failed: {ex.Message}";
        }

        return RedirectToAction(nameof(AllMembers));
    }

}

