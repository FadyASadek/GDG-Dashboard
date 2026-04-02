using GDG_DashBoard.BLL.Services.RoadmapServices;
using GDG_DashBoard.DAL.Models;
using GDGDashBoard.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.Controllers;

[Authorize(Roles = "Admin,Organizer,Speaker")]
public class RoadmapController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoadmapService _roadmapService;
    private readonly GDG_DashBoard.BLL.Services.QuizServices.IQuizService _quizService;

    public RoadmapController(AppDbContext context, UserManager<ApplicationUser> userManager, IRoadmapService roadmapService, GDG_DashBoard.BLL.Services.QuizServices.IQuizService quizService)
    {
        _context = context;
        _userManager = userManager;
        _roadmapService = roadmapService;
        _quizService = quizService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        IEnumerable<Roadmap> roadmaps;
        if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Organizer"))
        {
            roadmaps = await _roadmapService.GetAllRoadmapsAsync();
        }
        else
        {
            roadmaps = await _roadmapService.GetRoadmapsByCreatorAsync(user.Id);
        }
        
        return View(roadmaps);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(string title, string description, GDG_DashBoard.DAL.Eums.DifficultyLevel level, int estimatedTotalHours)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var roadmap = new Roadmap
        {
            Title = title,
            Description = description,
            Level = level,
            EstimatedTotalHours = estimatedTotalHours,
            CreatedByUserId = user.Id
        };
        _context.Roadmaps.Add(roadmap);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Roadmap created successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var roadmap = await _roadmapService.GetRoadmapDetailsAsync(id);
        if (roadmap == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Organizer"))
                ViewBag.AvailableQuizzes = await _quizService.GetAllQuizzesAsync();
            else
                ViewBag.AvailableQuizzes = await _quizService.GetQuizzesByCreatorAsync(user.Id);
        }

        return View(roadmap);
    }

    [HttpPost]
    public async Task<IActionResult> AddLevel(Guid roadmapId, string title, string? instructions, int orderIndex, Guid? quizId)
    {
        await _roadmapService.AddLevelAsync(roadmapId, title, instructions, orderIndex, quizId);
        TempData["SuccessMessage"] = "Level added successfully!";
        return RedirectToAction(nameof(Details), new { id = roadmapId });
    }

    [HttpPost]
    public async Task<IActionResult> AddResource(Guid roadmapId, Guid levelId, string title, string url, string? thumbnailUrl, int estimatedMinutes, int orderIndex, GDG_DashBoard.DAL.Eums.ResourceType type)
    {
        await _roadmapService.AddResourceAsync(levelId, title, url, thumbnailUrl, estimatedMinutes, orderIndex, type);
        TempData["SuccessMessage"] = "Resource added successfully!";
        return RedirectToAction(nameof(Details), new { id = roadmapId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLevel(Guid roadmapId, Guid levelId)
    {
        await _roadmapService.DeleteLevelAsync(levelId);
        TempData["SuccessMessage"] = "Level removed.";
        return RedirectToAction(nameof(Details), new { id = roadmapId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteResource(Guid roadmapId, Guid resourceId)
    {
        await _roadmapService.DeleteResourceAsync(resourceId);
        TempData["SuccessMessage"] = "Resource removed.";
        return RedirectToAction(nameof(Details), new { id = roadmapId });
    }
}
