namespace GDG_DashBoard.DAL.Models;

public class RoadmapLevel : BaseEntity
{
    public Guid RoadmapId { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(250)]
    public required string Title { get; set; }
    public string? Instructions { get; set; }
    public int OrderIndex { get; set; }

    public Roadmap Roadmap { get; set; } = null!;
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public ICollection<UserNodeProgress> UserProgresses { get; set; } = new List<UserNodeProgress>();

    public Guid? KnowledgeCheckQuizId { get; set; }
    public Quiz? KnowledgeCheckQuiz { get; set; }
}