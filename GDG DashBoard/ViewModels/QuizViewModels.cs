using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GDG_DashBoard.ViewModels.Quiz;

public class CreateQuizViewModel
{
    [Required, MaxLength(250)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    [Required, Range(1, 100)]
    public int PassingScorePercentage { get; set; } = 50;

    public List<QuizQuestionViewModel> Questions { get; set; } = new();
}

public class QuizQuestionViewModel
{
    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public List<QuizOptionViewModel> Options { get; set; } = new();
}

public class QuizOptionViewModel
{
    [Required, MaxLength(500)]
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
