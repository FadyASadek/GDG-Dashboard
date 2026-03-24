namespace GDG_DashBoard.BLL.ViewModels.Admin;

public class ManageUserRolesViewModel
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<RoleSelection> Roles { get; set; } = new();
}

public class RoleSelection
{
    public string RoleName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
