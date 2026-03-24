using GDG_DashBoard.BLL.Dtos.Auth;
using GDG_DashBoard.BLL.Services.Auth;
using GDG_DashBoard.DAL.Models;
using GDG_DashBoard.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GDG_DashBoard.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Admin");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var dto = new LoginDto
        {
            Email = model.Email,
            Password = model.Password,
            RememberMe = model.RememberMe
        };

        var result = await _authService.LoginAsync(dto);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        var model = new SetPasswordViewModel
        {
            Email = email,
            Token = token
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.SetPasswordAsync(model.Email, model.Token, model.Password);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Your password has been successfully set. You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to set password.");
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult CheckMyClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Json(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            UserName = User.Identity?.Name,
            Claims = claims,
            Roles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList()
        });
    }
}
