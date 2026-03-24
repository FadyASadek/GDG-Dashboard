using GDG_DashBoard.BLL.Dtos.Auth;
using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace GDG_DashBoard.BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null || !user.IsActive)
            return AuthResultDto.Fail("Invalid credentials or account is inactive.");

        var result = await _signInManager.PasswordSignInAsync(
            user,
            dto.Password,
            isPersistent: dto.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
            return AuthResultDto.Success();

        if (result.IsLockedOut)
            return AuthResultDto.Fail("Account is locked due to multiple failed attempts. Please try again later.");

        if (result.IsNotAllowed)
            return AuthResultDto.Fail("Login is not allowed for this account. Please contact your administrator.");

        return AuthResultDto.Fail("Invalid email or password.");
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<AuthResultDto> SetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return AuthResultDto.Fail("Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
            return AuthResultDto.Success();

        return AuthResultDto.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
