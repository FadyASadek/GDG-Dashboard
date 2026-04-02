namespace GDG_DashBoard.DAL.Models;

public class UserResourceProgress : BaseEntity
{
    public required Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public bool IsCompleted { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Resource Resource { get; set; } = null!;
}
