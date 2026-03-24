namespace GDG_DashBoard.DAL.Models;

public class Project : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }

    public UserProfile UserProfile { get; set; } = null!;
}