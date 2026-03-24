namespace GDG_DashBoard.BLL.Dtos.Admin;

public class OnboardMemberDto
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public string? Season { get; set; }
    public required string Role { get; set; }
}
