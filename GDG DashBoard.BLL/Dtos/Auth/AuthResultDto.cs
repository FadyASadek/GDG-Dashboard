namespace GDG_DashBoard.BLL.Dtos.Auth;

public class AuthResultDto
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Populated during OnboardMemberAsync.
    /// The admin shares this token with the new member so they can set their own password.
    /// </summary>
    public string? PasswordResetToken { get; set; }

    public static AuthResultDto Success() => new() { Succeeded = true };
    public static AuthResultDto Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
}
