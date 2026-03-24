using GDG_DashBoard.BLL.Dtos.Auth;

namespace GDG_DashBoard.BLL.Services.Auth;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto dto);
    Task LogoutAsync();
    Task<AuthResultDto> SetPasswordAsync(string email, string token, string newPassword);
}
