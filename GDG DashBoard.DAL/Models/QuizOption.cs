using System;
using System.ComponentModel.DataAnnotations;

namespace GDG_DashBoard.DAL.Models;

public class QuizOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    
    [MaxLength(500)]
    public required string Text { get; set; }
    
    public bool IsCorrect { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}
