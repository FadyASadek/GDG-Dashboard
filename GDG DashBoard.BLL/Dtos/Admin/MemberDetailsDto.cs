namespace GDG_DashBoard.BLL.Dtos.Admin;

/// <summary>Full member CV data — feeds the Member Profile page (code3.html)</summary>
public class MemberDetailsDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Season { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }

    // Profile
    public string? ProfessionalSummary { get; set; }
    public string? Location { get; set; }
    public string? GitHubUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ResumeFileUrl { get; set; }

    public IList<EducationDto> Educations { get; set; } = new List<EducationDto>();
    public IList<ExperienceDto> Experiences { get; set; } = new List<ExperienceDto>();
    public IList<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
    public IList<SkillDto> TechnicalSkills { get; set; } = new List<SkillDto>();
    public IList<SkillDto> SoftSkills { get; set; } = new List<SkillDto>();

    // Roadmap progress — feeds code1.html roadmap view
    public IList<EnrolledRoadmapDto> EnrolledRoadmaps { get; set; } = new List<EnrolledRoadmapDto>();
}

public class EducationDto
{
    public string Degree { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ExperienceDto
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
}

public class SkillDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class EnrolledRoadmapDto
{
    public Guid RoadmapId { get; set; }
    public string RoadmapTitle { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public IList<RoadmapLevelProgressDto> Levels { get; set; } = new List<RoadmapLevelProgressDto>();
}

public class RoadmapLevelProgressDto
{
    public Guid LevelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int OrderIndex { get; set; }
    public bool IsCompleted { get; set; }
}
