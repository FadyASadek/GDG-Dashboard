using System;

namespace GDG_DashBoard.DAL.Models;

public class UserQuizAttempt : BaseEntity
{
    public required Guid UserId { get; set; }
    public Guid QuizId { get; set; }
    
    public int ScorePercentage { get; set; }
    public bool IsPassed { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
}
