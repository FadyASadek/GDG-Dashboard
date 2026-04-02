using GDG_DashBoard.DAL.Models;
using GDGDashBoard.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDG_DashBoard.BLL.Services.QuizServices;

public class QuizService : IQuizService
{
    private readonly AppDbContext _context;

    public QuizService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Quiz>> GetAllQuizzesAsync()
    {
        return await _context.Quizzes
            .Include(q => q.CreatedBy)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Quiz>> GetQuizzesByCreatorAsync(Guid userId)
    {
        return await _context.Quizzes
            .Include(q => q.CreatedBy)
            .Where(q => q.CreatedByUserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<Quiz?> GetQuizDetailsAsync(Guid id)
    {
        return await _context.Quizzes
            .Include(q => q.CreatedBy)
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz, Guid creatorId)
    {
        quiz.CreatedByUserId = creatorId;
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> DeleteQuizAsync(Guid id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null) return false;

        // 1. Unlink from any RoadmapLevel that uses this quiz as a Knowledge Check
        var linkedLevels = await _context.RoadmapLevels
            .Where(l => l.KnowledgeCheckQuizId == id)
            .ToListAsync();
        foreach (var level in linkedLevels)
            level.KnowledgeCheckQuizId = null;

        // 2. Remove related quiz attempts (FK: Restrict)
        var attempts = await _context.UserQuizAttempts.Where(a => a.QuizId == id).ToListAsync();
        if (attempts.Any()) _context.UserQuizAttempts.RemoveRange(attempts);

        // 3. Remove resource progress for quiz-type resources pointing to this quiz
        var quizResources = await _context.Resources
            .Where(r => r.Url.Contains($"/Quiz/TakeQuiz/{id}"))
            .Select(r => r.Id)
            .ToListAsync();
        if (quizResources.Any())
        {
            var resProgress = await _context.UserResourceProgresses
                .Where(rp => quizResources.Contains(rp.ResourceId))
                .ToListAsync();
            if (resProgress.Any()) _context.UserResourceProgresses.RemoveRange(resProgress);
        }

        // 4. Delete the quiz (cascade deletes Questions + Options)
        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateQuizAsync(Guid id, Quiz updated)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return false;

        // Step 1: Update top-level fields
        quiz.Title = updated.Title;
        quiz.Description = updated.Description;
        quiz.PassingScorePercentage = updated.PassingScorePercentage;

        // Step 2: Delete old questions & options (cascade handles options)
        _context.QuizQuestions.RemoveRange(quiz.Questions);
        await _context.SaveChangesAsync();   // flush deletions first

        // Step 3: Insert new questions with correct QuizId
        foreach (var q in updated.Questions)
        {
            q.QuizId = id;
            foreach (var o in q.Options)
                o.QuestionId = q.Id == Guid.Empty ? q.Id : q.Id; // will be set by EF via navigation
            _context.QuizQuestions.Add(q);
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Quiz?> GetQuizWithQuestionsAsync(Guid id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                .ThenInclude(qq => qq.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        
        return quiz;
    }

    public async Task<UserQuizAttempt> SubmitQuizAttemptAsync(Guid userId, Guid quizId, Dictionary<Guid, Guid> questionAnswers)
    {
        var quiz = await GetQuizWithQuestionsAsync(quizId);
        if (quiz == null) throw new Exception("Quiz not found");

        int correctAnswers = 0;
        int totalQuestions = quiz.Questions.Count;

        foreach (var q in quiz.Questions)
        {
            var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);
            if (correctOption != null && questionAnswers.TryGetValue(q.Id, out var selectedOptionId))
            {
                if (selectedOptionId == correctOption.Id)
                {
                    correctAnswers++;
                }
            }
        }

        int scorePct = totalQuestions > 0 ? (int)Math.Round((double)correctAnswers / totalQuestions * 100) : 0;
        bool isPassed = scorePct >= quiz.PassingScorePercentage;

        var attempt = new UserQuizAttempt
        {
            UserId = userId,
            QuizId = quizId,
            ScorePercentage = scorePct,
            IsPassed = isPassed
        };

        _context.UserQuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return attempt;
    }

    public async Task<UserQuizAttempt?> GetLatestAttemptAsync(Guid userId, Guid quizId)
    {
        return await _context.UserQuizAttempts
            .Where(a => a.UserId == userId && a.QuizId == quizId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserQuizAttempt>> GetUserQuizAttemptsAsync(Guid userId)
    {
        return await _context.UserQuizAttempts
            .Include(a => a.Quiz)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
