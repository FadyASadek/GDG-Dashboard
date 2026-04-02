namespace GDG_DashBoard.BLL.Dtos.Member;

public class ToggleProgressResultDto
{
    public bool Success { get; set; }
    public bool IsCompleted { get; set; }
    public int CompletedCount { get; set; }
    public int TotalLevels { get; set; }
    public int Percentage { get; set; }
    public int ResourcePercentage { get; set; }
    public int CompletedResources { get; set; }
    public int TotalResources { get; set; }
}

public class ToggleResourceResultDto
{
    public bool Success { get; set; }
    public bool IsCompleted { get; set; }
    public bool LevelAutoCompleted { get; set; }
    public int LevelPercentage { get; set; }
    public int ResourcePercentage { get; set; }
    public int CompletedResources { get; set; }
    public int TotalResources { get; set; }
    public int CompletedLevels { get; set; }
    public int TotalLevels { get; set; }
}

public class RoadmapDetailsForMemberDto
{
    public DAL.Models.Roadmap Roadmap { get; set; } = null!;
    public HashSet<Guid> CompletedLevelIds { get; set; } = new();
    public HashSet<Guid> CompletedResourceIds { get; set; } = new();
    /// <summary>QuizId → (IsPassed, ScorePercentage). Null if never attempted.</summary>
    public Dictionary<Guid, (bool IsPassed, int Score)> QuizAttemptStatus { get; set; } = new();
    public int TotalLevels { get; set; }
    public int CompletedCount { get; set; }
    public int TotalResources { get; set; }
    public int CompletedResources { get; set; }
    public bool IsEnrolled { get; set; }
}
