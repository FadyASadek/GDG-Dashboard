namespace GDG_DashBoard.DAL.Models;

public class UserProfile : BaseEntity
{
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public string? ProfessionalSummary { get; set; }
    public string? Location { get; set; }
    public string? ResumeFileUrl { get; set; }
    public bool IsVerified { get; set; } = false;

    public string? GitHubUrl { get; set; }
    public string? LinkedInUrl { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
}