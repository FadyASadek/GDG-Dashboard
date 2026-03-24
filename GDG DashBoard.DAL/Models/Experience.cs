namespace GDG_DashBoard.DAL.Models;

public class Experience : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public required string Title { get; set; }
    public required string Company { get; set; }
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public UserProfile UserProfile { get; set; } = null!;
}