using System;
using System.Linq;
using System.Threading.Tasks;
using GDG_DashBoard.BLL.Services.QuizServices;
using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GDG_DashBoard.ViewModels.Quiz;

namespace GDG_DashBoard.Controllers;

[Authorize]
public class QuizController : Controller
{
    private readonly IQuizService _quizService;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuizController(IQuizService quizService, UserManager<ApplicationUser> userManager)
    {
        _quizService = quizService;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var quizzes = await _quizService.GetAllQuizzesAsync(); // For simplicity, we can show all or based on role
        if (!await _userManager.IsInRoleAsync(user, "Admin") && !await _userManager.IsInRoleAsync(user, "Organizer"))
        {
            quizzes = await _quizService.GetQuizzesByCreatorAsync(user.Id);
        }
        
        return View(quizzes);
    }

    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public IActionResult Create()
    {
        return View(new CreateQuizViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Create([FromBody] CreateQuizViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var quiz = new Quiz
        {
            Title = model.Title,
            Description = model.Description,
            PassingScorePercentage = model.PassingScorePercentage,
            CreatedByUserId = user.Id,
            Questions = model.Questions.Select(q => new QuizQuestion
            {
                Content = q.Content,
                OrderIndex = q.OrderIndex,
                Options = q.Options.Select(o => new QuizOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
        };

        await _quizService.CreateQuizAsync(quiz, user.Id);
        
        return Json(new { success = true, redirectUrl = Url.Action("Index", "Quiz") });
    }

    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Details(Guid id)
    {
        var quiz = await _quizService.GetQuizDetailsAsync(id);
        if (quiz == null) return NotFound();
        return View(quiz);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var quiz = await _quizService.GetQuizDetailsAsync(id);
        if (quiz == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        // Only creator or Admin can edit
        bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin && quiz.CreatedByUserId != user.Id)
            return Forbid();

        return View(quiz);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] GDG_DashBoard.ViewModels.Quiz.CreateQuizViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var updated = new Quiz
        {
            Title = model.Title,
            Description = model.Description,
            PassingScorePercentage = model.PassingScorePercentage,
            CreatedByUserId = user.Id,
            Questions = model.Questions.Select(q => new QuizQuestion
            {
                Content = q.Content,
                OrderIndex = q.OrderIndex,
                Options = q.Options.Select(o => new QuizOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
        };

        var result = await _quizService.UpdateQuizAsync(id, updated);
        if (!result) return NotFound();

        return Json(new { success = true, redirectUrl = Url.Action("Details", "Quiz", new { id }) });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer,Mentor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var quiz = await _quizService.GetQuizDetailsAsync(id);
        if (quiz == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin && quiz.CreatedByUserId != user.Id)
            return Forbid();

        await _quizService.DeleteQuizAsync(id);
        TempData["SuccessMessage"] = $"Quiz \"{quiz.Title}\" deleted.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize] // It has its own role check or challenge below. Wait, it's inside [Authorize] controller, so it's fine.
    public async Task<IActionResult> TakeQuiz(Guid id, Guid? roadmapId, Guid? levelId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var quiz = await _quizService.GetQuizWithQuestionsAsync(id);
        if (quiz == null) return NotFound();

        ViewBag.RoadmapId = roadmapId;
        ViewBag.LevelId = levelId;
        
        var attempt = await _quizService.GetLatestAttemptAsync(user.Id, id);
        if (attempt != null && attempt.IsPassed)
        {
            ViewBag.AlreadyPassed = true;
            ViewBag.Score = attempt.ScorePercentage;
        }

        return View(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitQuiz(Guid id, Guid? roadmapId, Guid? levelId, Microsoft.AspNetCore.Http.IFormCollection form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var dictionary = new System.Collections.Generic.Dictionary<Guid, Guid>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("q_") && Guid.TryParse(key.Substring(2), out Guid qId))
            {
                if (Guid.TryParse(form[key], out Guid optId))
                {
                    dictionary[qId] = optId;
                }
            }
        }

        var attempt = await _quizService.SubmitQuizAttemptAsync(user.Id, id, dictionary);
        
        TempData["QuizResult"] = attempt.IsPassed ? "Passed" : "Failed";
        TempData["QuizScore"] = attempt.ScorePercentage;

        if (attempt.IsPassed)
        {
            var memberSvc = HttpContext.RequestServices.GetService(typeof(GDG_DashBoard.BLL.Services.Member.IMemberService))
                            as GDG_DashBoard.BLL.Services.Member.IMemberService;
            if (memberSvc != null)
            {
                // Always sync quiz resource progress (handles resource-type quizzes + enrollment %)
                await memberSvc.SyncQuizResourceProgressAsync(user.Id, id);

                // Additionally, if this quiz is a knowledge-check for a specific level, mark that level done
                if (levelId.HasValue)
                    await memberSvc.ToggleNodeProgressAsync(user.Id, levelId.Value);
            }
        }

        if (roadmapId.HasValue)
        {
            return RedirectToAction("RoadmapDetails", "Member", new { id = roadmapId.Value });
        }
        else
        {
            // If they are previewing, redirect to the Quiz details or index.
            if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Organizer") || await _userManager.IsInRoleAsync(user, "Mentor"))
            {
                return RedirectToAction("Details", "Quiz", new { id = id });
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
