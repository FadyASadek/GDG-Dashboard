using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GDG_DashBoard.DAL.Models;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }
    
    [MaxLength(1000)]
    public required string Content { get; set; }

    public int OrderIndex { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}
