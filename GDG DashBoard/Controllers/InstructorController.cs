using GDG_DashBoard.BLL.Services.Group;
using GDG_DashBoard.DAL.Models;
using GDGDashBoard.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.Controllers;

[Authorize]
public class InstructorController : Controller
{
    private readonly IGroupService _groupService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public InstructorController(IGroupService groupService, UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _groupService = groupService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var groups = await _groupService.GetGroupsForInstructorAsync(user.Id);
        ViewBag.TotalInstructorRoadmaps = await _context.Roadmaps.CountAsync(r => r.CreatedByUserId == user.Id);
        
        return View(groups);
    }

  
}
