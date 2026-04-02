namespace GDG_DashBoard.BLL.ViewModels.Member;

public class MemberProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfessionalSummary { get; set; }
    public string? Location { get; set; }
    public string? GitHubUrl { get; set; }
    public string? LinkedInUrl { get; set; }

    // AI CV fields mapped structurally
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public List<ExperienceViewModel> Experiences { get; set; } = new();
    public List<EducationViewModel> Educations { get; set; } = new();
    public List<ProjectViewModel> Projects { get; set; } = new();

    public List<string> TechnicalSkills { get; set; } = new();
    public List<string> SoftSkills { get; set; } = new();

    public List<ProfileActiveRoadmapViewModel> ActiveRoadmaps { get; set; } = new();
    public List<ProfileCompletedRoadmapViewModel> CompletedRoadmaps { get; set; } = new();
    public List<ProfilePassedQuizViewModel> PassedQuizzes { get; set; } = new();
    public List<ProfileCompletedLevelViewModel> CompletedLevels { get; set; } = new();
}

public class ExperienceViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Period { get; set; }
    public string? Description { get; set; }
}

public class EducationViewModel
{
    public string Degree { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string? Year { get; set; }
}

public class ProjectViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Period { get; set; }
    public string? Url { get; set; }
}

public class ProfileActiveRoadmapViewModel
{
    public Guid RoadmapId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class ProfileCompletedRoadmapViewModel
{
    public Guid RoadmapId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

public class ProfilePassedQuizViewModel
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime PassedAt { get; set; }
}

public class ProfileCompletedLevelViewModel
{
    public Guid LevelId { get; set; }
    public Guid RoadmapId { get; set; }
    public string RoadmapTitle { get; set; } = string.Empty;
    public string LevelTitle { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
