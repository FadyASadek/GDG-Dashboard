using System;

namespace GDG_DashBoard.DAL.Models;

public class QuizViolation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuizId { get; set; }
    
    // Examples: "Tab Switch", "Copy/Paste", "Right-Click context menu"
    public required string ViolationType { get; set; }
}
