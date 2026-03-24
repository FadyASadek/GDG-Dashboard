namespace GDG_DashBoard.DAL.Models;

public class UserNodeProgress : BaseEntity
{
    public required Guid UserId { get; set; }
    public Guid RoadmapLevelId { get; set; }
    public bool IsCompleted { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public RoadmapLevel RoadmapLevel { get; set; } = null!;
}