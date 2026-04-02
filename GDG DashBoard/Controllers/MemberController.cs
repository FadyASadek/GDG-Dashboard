using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GDG_DashBoard.BLL.Services.Member;
using GDG_DashBoard.BLL.Services.Ai;
using GDG_DashBoard.BLL.ViewModels.Member;
using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace GDG_DashBoard.Controllers;

[Authorize]
public class MemberController : Controller
{
    private readonly IMemberService _memberService;
    private readonly ICvParserService _cvParserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GDG_DashBoard.BLL.Services.QuizServices.IQuizService _quizService;

    public MemberController(
        IMemberService memberService, 
        ICvParserService cvParserService, 
        UserManager<ApplicationUser> userManager,
        GDG_DashBoard.BLL.Services.QuizServices.IQuizService quizService)
    {
        _memberService = memberService;
        _cvParserService = cvParserService;
        _userManager = userManager;
        _quizService = quizService;
    }

    /// <summary>
    /// Unified Member landing page: Profile + Dashboard widgets combined.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        var vm = await _memberService.GetProfileForEditAsync(user.Id);
        var dashboard = await _memberService.GetMemberDashboardAsync(user.Id);
        ViewBag.Dashboard = dashboard;
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> MyRoadmaps()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var dashboard = await _memberService.GetMemberDashboardAsync(user.Id);
        if (dashboard == null) return NotFound();

        return View(dashboard);
    }

    /// <summary>
    /// Interactive Roadmap Progression View with level-by-level stepper.
    /// All data fetching and progress aggregation handled by IMemberService.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RoadmapDetails(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _memberService.GetRoadmapDetailsForMemberAsync(id, user.Id);
        if (result == null) return NotFound();

        ViewBag.CompletedLevelIds = result.CompletedLevelIds;
        ViewBag.TotalLevels = result.TotalLevels;
        ViewBag.CompletedCount = result.CompletedCount;
        ViewBag.IsEnrolled = result.IsEnrolled;
        ViewBag.CompletedResourceIds = result.CompletedResourceIds;
        ViewBag.QuizAttemptStatus = result.QuizAttemptStatus;
        ViewBag.TotalResources = result.TotalResources;
        ViewBag.CompletedResources = result.CompletedResources;

        return View(result.Roadmap);
    }

    /// <summary>
    /// AJAX endpoint: Toggle a level's completion status.
    /// All state mutation and progress recalculation handled by IMemberService.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleNodeProgress([FromBody] ToggleProgressRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _memberService.ToggleNodeProgressAsync(user.Id, request.LevelId);

        return Json(new
        {
            success = result.Success,
            isCompleted = result.IsCompleted,
            completedCount = result.CompletedCount,
            totalLevels = result.TotalLevels,
            percentage = result.Percentage
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        var vm = await _memberService.GetProfileForEditAsync(user.Id);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditProfile(MemberProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        
        if (ModelState.IsValid)
        {
            await _memberService.UpdateMemberProfileAsync(user.Id, model);
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("MyProfile");
        }
        return View(model);
    }
    
    [HttpPost("Member/ParseCV")]
    public async Task<IActionResult> ParseCV(IFormFile cvFile)
    {
        if (cvFile == null || cvFile.Length == 0) return BadRequest(new { error = "File is empty" });
        try 
        {
            string json = await _cvParserService.ParseCvToJsonAsync(cvFile);
            return Content(json, "application/json"); 
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyQuizAttempts()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var attempts = await _quizService.GetUserQuizAttemptsAsync(user.Id);
        return View(attempts);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleResourceProgress([FromBody] ToggleResourceRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _memberService.ToggleResourceProgressAsync(user.Id, request.ResourceId);

        return Json(new
        {
            success = result.Success,
            isCompleted = result.IsCompleted,
            levelAutoCompleted = result.LevelAutoCompleted,
            levelPercentage = result.LevelPercentage,
            resourcePercentage = result.ResourcePercentage,
            completedResources = result.CompletedResources,
            totalResources = result.TotalResources,
            completedLevels = result.CompletedLevels,
            totalLevels = result.TotalLevels
        });
    }
}

public class ToggleProgressRequest
{
    public Guid LevelId { get; set; }
}

public class ToggleResourceRequest
{
    public Guid ResourceId { get; set; }
}
