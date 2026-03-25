using GDG_DashBoard.DAL.Eums;

namespace GDG_DashBoard.DAL.Models;

public class Roadmap : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }

    // FIX: Was incorrectly typed as string. Must be Guid to match IdentityUser<Guid> PK.
    public required Guid CreatedByUserId { get; set; }
    public DifficultyLevel Level { get; set; }
    public int EstimatedTotalHours { get; set; }

    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<RoadmapLevel> Levels { get; set; } = new List<RoadmapLevel>();
    public ICollection<UserEnrollment> Enrollments { get; set; } = new List<UserEnrollment>();
    public ICollection<CommunityGroup> Groups { get; set; } = new List<CommunityGroup>();
}