using GDG_DashBoard.BLL.Services.Group;
using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.Controllers;

[Authorize]
public class GroupController : Controller
{
    private readonly IGroupService _groupService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroupController(IGroupService groupService, UserManager<ApplicationUser> userManager)
    {
        _groupService = groupService;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Create(string name, string description)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        await _groupService.CreateGroupAsync(name, description, user.Id);
        TempData["SuccessMessage"] = "Group created successfully!";
        return RedirectToAction("Index", "Instructor"); 
    }

    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Details(Guid id)
    {
        var group = await _groupService.GetGroupDetailsAsync(id);
        if (group == null) return NotFound();

        ViewBag.AvailableUsers = await _groupService.GetAvailableUsersAsync();
        ViewBag.AvailableRoadmaps = await _groupService.GetAvailableRoadmapsAsync();

        return View(group);
    }

    /// <summary>
    /// Read-only cohort view for standard Members. No management controls.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ViewCohort(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var group = await _groupService.GetGroupDetailsAsync(id);
        if (group == null) return NotFound();

        // Verify the member actually belongs to this group
        var isMember = group.GroupMembers?.Any(gm => gm.MemberId == user.Id) ?? false;
        var isManager = await _userManager.IsInRoleAsync(user, "Admin") 
                     || await _userManager.IsInRoleAsync(user, "Organizer") 
                     || await _userManager.IsInRoleAsync(user, "Mentor");

        if (!isMember && !isManager)
            return Forbid();

        bool isEnrolled = false;
        if (group.RoadmapId.HasValue)
        {
            var memberSvc = HttpContext.RequestServices.GetService(typeof(GDG_DashBoard.BLL.Services.Member.IMemberDashboardService)) as GDG_DashBoard.BLL.Services.Member.IMemberDashboardService;
            if (memberSvc != null)
            {
                var dashboard = await memberSvc.GetMemberDashboardAsync(user.Id);
                isEnrolled = dashboard?.ActiveRoadmaps.Any(r => r.RoadmapId == group.RoadmapId.Value) ?? false;
            }
        }
        ViewBag.IsEnrolled = isEnrolled;

        return View(group);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> AddMembers(Guid groupId, List<Guid> memberIds)
    {
        if (memberIds != null && memberIds.Any())
        {
            await _groupService.AddMembersToGroupBulkAsync(groupId, memberIds);
            TempData["SuccessMessage"] = "Members added successfully!";
        }
        return RedirectToAction(nameof(Details), new { id = groupId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> AssignRoadmap(Guid groupId, Guid roadmapId)
    {
        await _groupService.AssignRoadmapToGroupAsync(groupId, roadmapId);
        TempData["SuccessMessage"] = "Roadmap assigned and enrollments triggered successfully!";
        return RedirectToAction(nameof(Details), new { id = groupId });
    }

    [HttpGet]
    public async Task<IActionResult> Join(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            // Check if user is already in this group
            var groups = await _groupService.GetAllGroupsAsync();
            var existingGroup = groups.FirstOrDefault(g => g.JoinCode == code);
            if (existingGroup != null && existingGroup.GroupMembers.Any(gm => gm.MemberId == user.Id))
            {
                return RedirectToAction("ViewCohort", new { id = existingGroup.Id });
            }
        }

        ViewBag.JoinCode = code;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> JoinPost(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        try 
        {
            await _groupService.JoinGroupByCodeAsync(user.Id, code);
            TempData["SuccessMessage"] = "Successfully joined the group!";
            return RedirectToAction("MyProfile", "Member"); 
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Join", new { code });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> GroupViolations(Guid id)
    {
        var group = await _groupService.GetGroupDetailsAsync(id);
        if (group == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isManager = await _userManager.IsInRoleAsync(user, "Admin") 
                     || await _userManager.IsInRoleAsync(user, "Organizer") 
                     || await _userManager.IsInRoleAsync(user, "Mentor");
        if (!isManager) return Forbid();

        var userIds = group.GroupMembers.Select(gm => gm.MemberId).ToList();

        var quizViolationRepo = HttpContext.RequestServices.GetService(typeof(GDG_DashBoard.DAL.Repositores.GenericRepository.IGenericRepositoryAsync<GDG_DashBoard.DAL.Models.QuizViolation>)) as GDG_DashBoard.DAL.Repositores.GenericRepository.IGenericRepositoryAsync<GDG_DashBoard.DAL.Models.QuizViolation>;

        var violations = new List<GDG_DashBoard.DAL.Models.QuizViolation>();
        if (quizViolationRepo != null && userIds.Any())
        {
            violations = await quizViolationRepo.GetTableNoTracking()
                .Where(v => userIds.Contains(v.UserId))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        ViewBag.Users = users.ToDictionary(u => u.Id, u => u.Email);
        ViewBag.Group = group;

        return View(violations);
    }
}
