using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GDG_DashBoard.DAL.Models;

public class Quiz : BaseEntity
{
    [MaxLength(250)]
    public required string Title { get; set; }
    public string? Description { get; set; }
    
    [Range(0, 100)]
    public int PassingScorePercentage { get; set; } = 50;

    public required Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;

    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<UserQuizAttempt> Attempts { get; set; } = new List<UserQuizAttempt>();
    public ICollection<RoadmapLevel> RoadmapLevels { get; set; } = new List<RoadmapLevel>();
}
