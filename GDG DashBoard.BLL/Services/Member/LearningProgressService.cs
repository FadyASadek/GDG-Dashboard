using GDG_DashBoard.BLL.Dtos.Member;
using GDG_DashBoard.BLL.Services.RoadmapServices;
using GDG_DashBoard.DAL.Models;
using Microsoft.EntityFrameworkCore;
using GDG_DashBoard.DAL.Repositores.GenericRepository;

namespace GDG_DashBoard.BLL.Services.Member;

public class LearningProgressService : ILearningProgressService
{
    private readonly IGenericRepositoryAsync<UserEnrollment> _enrollmentRepo;
    private readonly IGenericRepositoryAsync<UserNodeProgress> _progressRepo;
    private readonly IGenericRepositoryAsync<RoadmapLevel> _levelRepo;
    private readonly IGenericRepositoryAsync<UserQuizAttempt> _quizAttemptRepo;
    private readonly IGenericRepositoryAsync<Resource> _resourceRepo;
    private readonly IGenericRepositoryAsync<UserResourceProgress> _resourceProgressRepo;
    private readonly IGenericRepositoryAsync<QuizViolation> _quizViolationRepo;
    private readonly IRoadmapService _roadmapService;

    public LearningProgressService(
        IGenericRepositoryAsync<UserEnrollment> enrollmentRepo,
        IGenericRepositoryAsync<UserNodeProgress> progressRepo,
        IGenericRepositoryAsync<RoadmapLevel> levelRepo,
        IGenericRepositoryAsync<UserQuizAttempt> quizAttemptRepo,
        IGenericRepositoryAsync<Resource> resourceRepo,
        IGenericRepositoryAsync<UserResourceProgress> resourceProgressRepo,
        IGenericRepositoryAsync<QuizViolation> quizViolationRepo,
        IRoadmapService roadmapService)
    {
        _enrollmentRepo = enrollmentRepo;
        _progressRepo = progressRepo;
        _levelRepo = levelRepo;
        _quizAttemptRepo = quizAttemptRepo;
        _resourceRepo = resourceRepo;
        _resourceProgressRepo = resourceProgressRepo;
        _quizViolationRepo = quizViolationRepo;
        _roadmapService = roadmapService;
    }

    public async Task EnrollInRoadmapAsync(Guid userId, Guid roadmapId)
    {
        var existingEnrollment = await _enrollmentRepo.GetTableNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.RoadmapId == roadmapId);

        if (existingEnrollment != null) return;

        var roadmapLevels = await _levelRepo.GetTableNoTracking()
            .Where(l => l.RoadmapId == roadmapId)
            .ToListAsync();

        var newEnrollment = new UserEnrollment
        {
            UserId = userId,
            RoadmapId = roadmapId,
            Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.InProgress,
            ProgressPercentage = 0
        };

        await _enrollmentRepo.AddAsync(newEnrollment);

        foreach (var level in roadmapLevels)
        {
            var nodeProgress = new UserNodeProgress
            {
                UserId = userId,
                RoadmapLevelId = level.Id,
                IsCompleted = false
            };
            await _progressRepo.AddAsync(nodeProgress);
        }

        await _enrollmentRepo.SaveChangesAsync();
        await _progressRepo.SaveChangesAsync();
    }

    public async Task<RoadmapDetailsForMemberDto?> GetRoadmapDetailsForMemberAsync(Guid roadmapId, Guid userId)
    {
        var roadmap = await _roadmapService.GetRoadmapDetailsAsync(roadmapId);
        if (roadmap == null) return null;

        var levelIds = roadmap.Levels.Select(l => l.Id).ToList();
        var progresses = await _progressRepo.GetTableNoTracking()
            .Where(p => p.UserId == userId && levelIds.Contains(p.RoadmapLevelId))
            .ToListAsync();

        var isEnrolled = await _enrollmentRepo.GetTableNoTracking()
            .AnyAsync(e => e.UserId == userId && e.RoadmapId == roadmapId);

        // Resource-grain tracking
        var allResourceIds = roadmap.Levels.SelectMany(l => l.Resources.Select(r => r.Id)).ToList();
        var resourceProgress = await _resourceProgressRepo.GetTableNoTracking()
            .Where(rp => rp.UserId == userId && allResourceIds.Contains(rp.ResourceId))
            .ToListAsync();

        // Quiz attempt status — collect from BOTH KnowledgeCheck IDs AND Quiz-type resource URLs
        var quizIds = roadmap.Levels
            .Select(l => l.KnowledgeCheckQuizId)
            .Where(q => q.HasValue)
            .Select(q => q!.Value)
            .ToHashSet();

        // Extract quiz IDs from Quiz-type resource URLs (pattern: /Quiz/TakeQuiz/{guid})
        foreach (var level in roadmap.Levels)
        {
            foreach (var res in level.Resources.Where(r => r.Type == GDG_DashBoard.DAL.Eums.ResourceType.Quiz))
            {
                var url = res.Url;
                var marker = "/Quiz/TakeQuiz/";
                var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var idStr = url.Substring(idx + marker.Length).Split('?', '#')[0].Trim();
                    if (Guid.TryParse(idStr, out var qId))
                        quizIds.Add(qId);
                }
            }
        }

        var quizAttempts = await _quizAttemptRepo.GetTableNoTracking()
            .Where(a => a.UserId == userId && quizIds.Contains(a.QuizId))
            .ToListAsync();
        var quizStatus = quizAttempts
            .GroupBy(a => a.QuizId)
            .ToDictionary(
                g => g.Key,
                g => {
                    var best = g.OrderByDescending(a => a.ScorePercentage).First();
                    return (best.IsPassed, best.ScorePercentage);
                });

        return new RoadmapDetailsForMemberDto
        {
            Roadmap = roadmap,
            CompletedLevelIds = progresses.Where(p => p.IsCompleted).Select(p => p.RoadmapLevelId).ToHashSet(),
            CompletedResourceIds = resourceProgress.Where(rp => rp.IsCompleted).Select(rp => rp.ResourceId).ToHashSet(),
            QuizAttemptStatus = quizStatus,
            TotalLevels = roadmap.Levels.Count,
            CompletedCount = progresses.Count(p => p.IsCompleted),
            TotalResources = allResourceIds.Count,
            CompletedResources = resourceProgress.Count(rp => rp.IsCompleted),
            IsEnrolled = isEnrolled
        };
    }

    public async Task<ToggleProgressResultDto> ToggleNodeProgressAsync(Guid userId, Guid levelId)
    {
        var level = await _levelRepo.GetTableNoTracking()
            .FirstOrDefaultAsync(l => l.Id == levelId);
            
        if (level == null) return new ToggleProgressResultDto { Success = false };

        var progress = await _progressRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.RoadmapLevelId == levelId);

        bool willBeCompleted = progress == null ? true : !progress.IsCompleted;

        // --- Knowledge Check Enforcement ---
        if (willBeCompleted && level.KnowledgeCheckQuizId.HasValue)
        {
            var attempt = await _quizAttemptRepo.GetTableNoTracking()
                .Where(a => a.UserId == userId && a.QuizId == level.KnowledgeCheckQuizId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (attempt == null || !attempt.IsPassed)
            {
                return new ToggleProgressResultDto { Success = false, IsCompleted = false };
            }
        }

        if (progress == null)
        {
            progress = new UserNodeProgress
            {
                UserId = userId,
                RoadmapLevelId = levelId,
                IsCompleted = true
            };
            await _progressRepo.AddAsync(progress);
        }
        else
        {
            progress.IsCompleted = willBeCompleted;
        }

        await _progressRepo.SaveChangesAsync();

        if (level != null)
        {
            var roadmapLevelIds = await _levelRepo.GetTableNoTracking()
                .Where(l => l.RoadmapId == level.RoadmapId)
                .Select(l => l.Id)
                .ToListAsync();

            var completedCount = await _progressRepo.GetTableNoTracking()
                .CountAsync(p => p.UserId == userId && roadmapLevelIds.Contains(p.RoadmapLevelId) && p.IsCompleted);

            var totalLevels = roadmapLevelIds.Count;
            var percentage = totalLevels > 0 ? (int)Math.Round((double)completedCount / totalLevels * 100) : 0;

            var enrollment = await _enrollmentRepo.GetTableAsTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.RoadmapId == level.RoadmapId);
            
            if (enrollment == null)
            {
                enrollment = new UserEnrollment
                {
                    UserId = userId,
                    RoadmapId = level.RoadmapId,
                    Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.InProgress,
                    ProgressPercentage = percentage
                };
                await _enrollmentRepo.AddAsync(enrollment);
            }
            else
            {
                enrollment.ProgressPercentage = percentage;
                if (percentage == 100) enrollment.Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.Completed;
            }
            await _enrollmentRepo.SaveChangesAsync();

            return new ToggleProgressResultDto
            {
                Success = true,
                IsCompleted = progress.IsCompleted,
                CompletedCount = completedCount,
                TotalLevels = totalLevels,
                Percentage = percentage
            };
        }

        return new ToggleProgressResultDto
        {
            Success = true,
            IsCompleted = progress.IsCompleted
        };
    }

    public async Task<ToggleResourceResultDto> ToggleResourceProgressAsync(Guid userId, Guid resourceId)
    {
        var resource = await _resourceRepo.GetTableNoTracking()
            .Include(r => r.RoadmapLevel)
            .FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return new ToggleResourceResultDto { Success = false };

        if (resource.RoadmapLevel.KnowledgeCheckQuizId.HasValue)
        {
            var attempt = await _quizAttemptRepo.GetTableNoTracking()
                .Where(a => a.UserId == userId && a.QuizId == resource.RoadmapLevel.KnowledgeCheckQuizId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (attempt == null || !attempt.IsPassed)
            {
                return new ToggleResourceResultDto { Success = false, ErrorMessage = "You must pass this Level's Gatekeeper Quiz first." };
            }
        }

        var rp = await _resourceProgressRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ResourceId == resourceId);

        bool willBeCompleted;
        if (rp == null)
        {
            rp = new UserResourceProgress { UserId = userId, ResourceId = resourceId, IsCompleted = true };
            await _resourceProgressRepo.AddAsync(rp);
            willBeCompleted = true;
        }
        else
        {
            rp.IsCompleted = !rp.IsCompleted;
            willBeCompleted = rp.IsCompleted;
        }
        await _resourceProgressRepo.SaveChangesAsync();

        var levelId = resource.RoadmapLevelId;
        var roadmapId = resource.RoadmapLevel.RoadmapId;

        // Check if all level resources are done => auto-complete the level
        var allLevelResources = await _resourceRepo.GetTableNoTracking()
            .Where(r => r.RoadmapLevelId == levelId)
            .Select(r => r.Id)
            .ToListAsync();
        var completedLevelResources = await _resourceProgressRepo.GetTableNoTracking()
            .Where(r => r.UserId == userId && allLevelResources.Contains(r.ResourceId) && r.IsCompleted)
            .CountAsync();
        bool allDone = allLevelResources.Count > 0 && completedLevelResources == allLevelResources.Count;

        // Auto-complete/uncomplete the level
        var levelProgress = await _progressRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.RoadmapLevelId == levelId);
        bool levelAutoCompleted = false;
        if (allDone && (levelProgress == null || !levelProgress.IsCompleted))
        {
            if (levelProgress == null)
            {
                levelProgress = new UserNodeProgress { UserId = userId, RoadmapLevelId = levelId, IsCompleted = true };
                await _progressRepo.AddAsync(levelProgress);
            }
            else levelProgress.IsCompleted = true;
            levelAutoCompleted = true;
        }
        else if (!allDone && levelProgress != null && levelProgress.IsCompleted)
        {
            levelProgress.IsCompleted = false;
        }
        await _progressRepo.SaveChangesAsync();

        // Recompute roadmap-level progress
        var allRoadmapLevelIds = await _levelRepo.GetTableNoTracking()
            .Where(l => l.RoadmapId == roadmapId).Select(l => l.Id).ToListAsync();
        var completedLevels = await _progressRepo.GetTableNoTracking()
            .CountAsync(p => p.UserId == userId && allRoadmapLevelIds.Contains(p.RoadmapLevelId) && p.IsCompleted);
        int totalLevels = allRoadmapLevelIds.Count;
        int levelPct = totalLevels > 0 ? (int)Math.Round((double)completedLevels / totalLevels * 100) : 0;

        // Recompute roadmap resource progress
        var allRoadmapResourceIds = await _resourceRepo.GetTableNoTracking()
            .Where(r => allRoadmapLevelIds.Contains(r.RoadmapLevelId)).Select(r => r.Id).ToListAsync();
        var completedResourcesCount = await _resourceProgressRepo.GetTableNoTracking()
            .CountAsync(rp2 => rp2.UserId == userId && allRoadmapResourceIds.Contains(rp2.ResourceId) && rp2.IsCompleted);
        int totalResources = allRoadmapResourceIds.Count;
        int resourcePct = totalResources > 0 ? (int)Math.Round((double)completedResourcesCount / totalResources * 100) : 0;

        // Update enrollment overall percentage = blend of level% and resource%
        var overallPct = (levelPct + resourcePct) / 2;
        var enrollment = await _enrollmentRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.RoadmapId == roadmapId);
        if (enrollment == null)
        {
            enrollment = new UserEnrollment
            {
                UserId = userId, RoadmapId = roadmapId,
                Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.InProgress,
                ProgressPercentage = overallPct
            };
            await _enrollmentRepo.AddAsync(enrollment);
        }
        else
        {
            enrollment.ProgressPercentage = overallPct;
            if (overallPct == 100) enrollment.Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.Completed;
        }
        await _enrollmentRepo.SaveChangesAsync();

        return new ToggleResourceResultDto
        {
            Success = true,
            IsCompleted = willBeCompleted,
            LevelAutoCompleted = levelAutoCompleted,
            LevelPercentage = levelPct,
            ResourcePercentage = resourcePct,
            CompletedResources = completedResourcesCount,
            TotalResources = totalResources,
            CompletedLevels = completedLevels,
            TotalLevels = totalLevels
        };
    }

    public async Task SyncQuizResourceProgressAsync(Guid userId, Guid quizId)
    {
        var marker = $"/Quiz/TakeQuiz/{quizId}";
        var quizResource = await _resourceRepo.GetTableNoTracking()
            .Include(r => r.RoadmapLevel)
            .FirstOrDefaultAsync(r =>
                r.Type == GDG_DashBoard.DAL.Eums.ResourceType.Quiz &&
                r.Url.Contains(marker));

        if (quizResource == null) return;

        var existingRp = await _resourceProgressRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(rp => rp.UserId == userId && rp.ResourceId == quizResource.Id);

        if (existingRp == null)
        {
            await _resourceProgressRepo.AddAsync(new UserResourceProgress
            {
                UserId = userId,
                ResourceId = quizResource.Id,
                IsCompleted = true
            });
        }
        else if (!existingRp.IsCompleted)
        {
            existingRp.IsCompleted = true;
        }
        else
        {
            return;
        }
        await _resourceProgressRepo.SaveChangesAsync();

        var levelId = quizResource.RoadmapLevelId;
        var roadmapId = quizResource.RoadmapLevel.RoadmapId;

        var allLevelResources = await _resourceRepo.GetTableNoTracking()
            .Where(r => r.RoadmapLevelId == levelId).Select(r => r.Id).ToListAsync();
        var completedLevelResources = await _resourceProgressRepo.GetTableNoTracking()
            .CountAsync(rp => rp.UserId == userId && allLevelResources.Contains(rp.ResourceId) && rp.IsCompleted);
        bool allDone = allLevelResources.Count > 0 && completedLevelResources == allLevelResources.Count;

        var levelProgress = await _progressRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.RoadmapLevelId == levelId);
        if (allDone && (levelProgress == null || !levelProgress.IsCompleted))
        {
            if (levelProgress == null)
            {
                await _progressRepo.AddAsync(new UserNodeProgress { UserId = userId, RoadmapLevelId = levelId, IsCompleted = true });
            }
            else levelProgress.IsCompleted = true;
            await _progressRepo.SaveChangesAsync();
        }

        // Recompute enrollment progress
        var allLevelIds = await _levelRepo.GetTableNoTracking()
            .Where(l => l.RoadmapId == roadmapId).Select(l => l.Id).ToListAsync();
        var completedLevels = await _progressRepo.GetTableNoTracking()
            .CountAsync(p => p.UserId == userId && allLevelIds.Contains(p.RoadmapLevelId) && p.IsCompleted);
        int totalLevels = allLevelIds.Count;
        int levelPct = totalLevels > 0 ? (int)Math.Round((double)completedLevels / totalLevels * 100) : 0;

        var allResIds = await _resourceRepo.GetTableNoTracking()
            .Where(r => allLevelIds.Contains(r.RoadmapLevelId)).Select(r => r.Id).ToListAsync();
        var completedRes = await _resourceProgressRepo.GetTableNoTracking()
            .CountAsync(rp => rp.UserId == userId && allResIds.Contains(rp.ResourceId) && rp.IsCompleted);
        int totalRes = allResIds.Count;
        int resPct = totalRes > 0 ? (int)Math.Round((double)completedRes / totalRes * 100) : 0;

        int overallPct = (levelPct + resPct) / 2;

        var enrollment = await _enrollmentRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.RoadmapId == roadmapId);
        if (enrollment == null)
        {
            await _enrollmentRepo.AddAsync(new UserEnrollment
            {
                UserId = userId, RoadmapId = roadmapId,
                Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.InProgress,
                ProgressPercentage = overallPct
            });
        }
        else
        {
            enrollment.ProgressPercentage = overallPct;
            if (overallPct == 100) enrollment.Status = GDG_DashBoard.DAL.Eums.EnrollmentStatus.Completed;
        }
        await _enrollmentRepo.SaveChangesAsync();
    }

    public async Task LogQuizViolationAsync(Guid userId, Guid quizId, string violationType)
    {
        var violation = new QuizViolation
        {
            UserId = userId,
            QuizId = quizId,
            ViolationType = violationType,
            CreatedAt = DateTime.UtcNow
        };
        await _quizViolationRepo.AddAsync(violation);
        await _quizViolationRepo.SaveChangesAsync();
    }
}
