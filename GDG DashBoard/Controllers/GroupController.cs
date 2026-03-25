using GDG_DashBoard.BLL.Services.Group;
using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, string description)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        await _groupService.CreateGroupAsync(name, description, user.Id);
        TempData["SuccessMessage"] = "Group created successfully!";
        return RedirectToAction("Index", "Instructor"); 
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var group = await _groupService.GetGroupDetailsAsync(id);
        if (group == null) return NotFound();

        ViewBag.AvailableUsers = await _groupService.GetAvailableUsersAsync();
        ViewBag.AvailableRoadmaps = await _groupService.GetAvailableRoadmapsAsync();

        return View(group);
    }

    [HttpPost]
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
    public async Task<IActionResult> AssignRoadmap(Guid groupId, Guid roadmapId)
    {
        await _groupService.AssignRoadmapToGroupAsync(groupId, roadmapId);
        TempData["SuccessMessage"] = "Roadmap assigned and enrollments triggered successfully!";
        return RedirectToAction(nameof(Details), new { id = groupId });
    }

    [HttpGet]
    public IActionResult Join(string code)
    {
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
            return RedirectToAction("Index", "Home"); 
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Join", new { code });
        }
    }
}
