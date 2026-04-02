using GDG_DashBoard.DAL.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace GDG_DashBoard.BLL.Services.QuizServices;

public interface IQuizService
{
    Task<List<Quiz>> GetAllQuizzesAsync();
    Task<List<Quiz>> GetQuizzesByCreatorAsync(Guid userId);
    Task<Quiz?> GetQuizDetailsAsync(Guid id);
    Task<Quiz> CreateQuizAsync(Quiz quiz, Guid creatorId);
    Task<bool> UpdateQuizAsync(Guid id, Quiz updated);
    Task<bool> DeleteQuizAsync(Guid id);
    
    // For Members
    Task<Quiz?> GetQuizWithQuestionsAsync(Guid id);
    Task<UserQuizAttempt> SubmitQuizAttemptAsync(Guid userId, Guid quizId, Dictionary<Guid, Guid> questionAnswers);
    Task<UserQuizAttempt?> GetLatestAttemptAsync(Guid userId, Guid quizId);
    Task<List<UserQuizAttempt>> GetUserQuizAttemptsAsync(Guid userId);
}
