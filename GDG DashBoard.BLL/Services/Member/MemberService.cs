using GDG_DashBoard.BLL.Dtos.Member;
using GDG_DashBoard.BLL.ViewModels.Member;
using GDG_DashBoard.BLL.Services.RoadmapServices;
using GDG_DashBoard.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GDG_DashBoard.DAL.Repositores.GenericRepository;

namespace GDG_DashBoard.BLL.Services.Member;

public class MemberService : IMemberService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGenericRepositoryAsync<UserProfile> _profileRepo;
    private readonly IGenericRepositoryAsync<GroupMember> _groupMemberRepo;
    private readonly IGenericRepositoryAsync<UserEnrollment> _enrollmentRepo;
    private readonly IGenericRepositoryAsync<Experience> _expRepo;
    private readonly IGenericRepositoryAsync<Education> _eduRepo;
    private readonly IGenericRepositoryAsync<Project> _projRepo;
    private readonly IGenericRepositoryAsync<UserSkill> _skillRepo;
    private readonly IGenericRepositoryAsync<UserNodeProgress> _progressRepo;
    private readonly IGenericRepositoryAsync<RoadmapLevel> _levelRepo;
    private readonly IGenericRepositoryAsync<UserQuizAttempt> _quizAttemptRepo;
    private readonly IGenericRepositoryAsync<Resource> _resourceRepo;
    private readonly IGenericRepositoryAsync<UserResourceProgress> _resourceProgressRepo;
    private readonly IRoadmapService _roadmapService;

    public MemberService(
        UserManager<ApplicationUser> userManager,
        IGenericRepositoryAsync<UserProfile> profileRepo,
        IGenericRepositoryAsync<GroupMember> groupMemberRepo,
        IGenericRepositoryAsync<UserEnrollment> enrollmentRepo,
        IGenericRepositoryAsync<Experience> expRepo,
        IGenericRepositoryAsync<Education> eduRepo,
        IGenericRepositoryAsync<Project> projRepo,
        IGenericRepositoryAsync<UserSkill> skillRepo,
        IGenericRepositoryAsync<UserNodeProgress> progressRepo,
        IGenericRepositoryAsync<RoadmapLevel> levelRepo,
        IGenericRepositoryAsync<UserQuizAttempt> quizAttemptRepo,
        IGenericRepositoryAsync<Resource> resourceRepo,
        IGenericRepositoryAsync<UserResourceProgress> resourceProgressRepo,
        IRoadmapService roadmapService)
    {
        _userManager = userManager;
        _profileRepo = profileRepo;
        _groupMemberRepo = groupMemberRepo;
        _enrollmentRepo = enrollmentRepo;
        _expRepo = expRepo;
        _eduRepo = eduRepo;
        _projRepo = projRepo;
        _skillRepo = skillRepo;
        _progressRepo = progressRepo;
        _levelRepo = levelRepo;
        _quizAttemptRepo = quizAttemptRepo;
        _resourceRepo = resourceRepo;
        _resourceProgressRepo = resourceProgressRepo;
        _roadmapService = roadmapService;
    }

    public async Task<MemberDashboardDto?> GetMemberDashboardAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var profile = await _profileRepo.GetTableNoTracking()
            .Include(p => p.Experiences)
            .Include(p => p.Educations)
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var groups = await _groupMemberRepo.GetTableNoTracking()
            .Include(gm => gm.Group)
                .ThenInclude(g => g.Roadmap)
            .Where(gm => gm.MemberId == userId)
            .Select(gm => new ActiveGroupDto
            {
                GroupId = gm.GroupId,
                Name = gm.Group.Name,
                RoadmapTitle = gm.Group.Roadmap != null ? gm.Group.Roadmap.Title : null
            })
            .ToListAsync();

        var enrollments = await _enrollmentRepo.GetTableNoTracking()
            .Include(e => e.Roadmap)
            .Where(e => e.UserId == userId)
            .Select(e => new ActiveRoadmapDto
            {
                RoadmapId = e.RoadmapId,
                Title = e.Roadmap.Title,
                ProgressPercentage = e.ProgressPercentage,
                Status = e.Status.ToString()
            })
            .ToListAsync();

        int completeness = 20; 
        if (profile != null)
        {
            if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary)) completeness += 20;
            if (profile.Experiences.Any()) completeness += 20;
            if (profile.Educations.Any()) completeness += 20;
            if (profile.Skills.Any()) completeness += 20;
        }

        return new MemberDashboardDto
        {
            UserId = userId,
            FullName = profile?.FullName ?? user.Email ?? "Member",
            ProfessionalSummary = profile?.ProfessionalSummary,
            ProfileCompletenessPercentage = completeness,
            ActiveGroups = groups,
            ActiveRoadmaps = enrollments
        };
    }

    public async Task<MemberProfileViewModel> GetProfileForEditAsync(Guid userId)
    {
        var profile = await _profileRepo.GetTableNoTracking()
            .Include(p => p.Experiences)
            .Include(p => p.Educations)
            .Include(p => p.Projects)
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var vm = new MemberProfileViewModel();
        if (profile == null) 
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            vm.FullName = user?.Email ?? "New User";
            vm.Email = user?.Email;
            return vm;
        }

        vm.FullName = profile.FullName;
        vm.Email = profile.ContactEmail;
        vm.Phone = profile.Phone;
        vm.Location = profile.Location;
        vm.ProfessionalSummary = profile.ProfessionalSummary;
        vm.GitHubUrl = profile.GitHubUrl;
        vm.LinkedInUrl = profile.LinkedInUrl;

        vm.Experiences = profile.Experiences.OrderByDescending(e => e.CreatedAt).Select(e => new ExperienceViewModel
        {
            Title = e.Title,
            Company = e.Company,
            Period = null, 
            Description = e.Description
        }).ToList();

        vm.Educations = profile.Educations.OrderByDescending(e => e.CreatedAt).Select(e => new EducationViewModel
        {
            Degree = e.Degree,
            University = e.University,
            Year = e.StartDate.Year.ToString()
        }).ToList();

        vm.Projects = profile.Projects.OrderByDescending(p => p.CreatedAt).Select(p => new ProjectViewModel
        {
            Name = p.Name,
            Description = p.Description,
            Period = p.Period,
            Url = p.Url
        }).ToList();

        vm.TechnicalSkills = profile.Skills.Where(s => s.Type == GDG_DashBoard.DAL.Eums.SkillType.Technical).Select(s => s.Name).ToList();
        vm.SoftSkills = profile.Skills.Where(s => s.Type == GDG_DashBoard.DAL.Eums.SkillType.SoftSkill).Select(s => s.Name).ToList();

        // 1. Fetch Enrollments for Roadmaps
        var enrollments = await _enrollmentRepo.GetTableNoTracking()
            .Include(e => e.Roadmap)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        vm.ActiveRoadmaps = enrollments
            .Where(e => e.ProgressPercentage < 100)
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            .Select(e => new ProfileActiveRoadmapViewModel
            {
                RoadmapId = e.RoadmapId,
                Title = e.Roadmap?.Title ?? "Unknown Roadmap",
                ProgressPercentage = e.ProgressPercentage,
                LastUpdatedAt = e.UpdatedAt ?? e.CreatedAt
            }).ToList();

        vm.CompletedRoadmaps = enrollments
            .Where(e => e.ProgressPercentage == 100)
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            .Select(e => new ProfileCompletedRoadmapViewModel
            {
                RoadmapId = e.RoadmapId,
                Title = e.Roadmap?.Title ?? "Unknown Roadmap",
                CompletedAt = e.UpdatedAt ?? e.CreatedAt
            }).ToList();

        // 2. Fetch Completed Levels
        var completedLevels = await _progressRepo.GetTableNoTracking()
            .Include(p => p.RoadmapLevel)
                .ThenInclude(l => l.Roadmap)
            .Where(p => p.UserId == userId && p.IsCompleted)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync();

        vm.CompletedLevels = completedLevels.Select(p => new ProfileCompletedLevelViewModel
        {
            LevelId = p.RoadmapLevelId,
            RoadmapId = p.RoadmapLevel?.RoadmapId ?? Guid.Empty,
            LevelTitle = p.RoadmapLevel?.Title ?? "Unknown Level",
            RoadmapTitle = p.RoadmapLevel?.Roadmap?.Title ?? "Unknown Roadmap",
            CompletedAt = p.UpdatedAt ?? p.CreatedAt
        }).ToList();

        // 3. Fetch Passed Quizzes
        var passedQuizzes = await _quizAttemptRepo.GetTableNoTracking()
            .Include(q => q.Quiz)
            .Where(q => q.UserId == userId && q.IsPassed)
            .ToListAsync();

        // Group by QuizId and get the best score or latest passed
        var topPassedQuizzes = passedQuizzes
            .GroupBy(q => q.QuizId)
            .Select(g => g.OrderByDescending(q => q.ScorePercentage).First())
            .OrderByDescending(q => q.CreatedAt)
            .ToList();

        vm.PassedQuizzes = topPassedQuizzes.Select(q => new ProfilePassedQuizViewModel
        {
            QuizId = q.QuizId,
            Title = q.Quiz?.Title ?? "Unknown Quiz",
            Score = q.ScorePercentage,
            PassedAt = q.CreatedAt
        }).ToList();

        return vm;
    }

    public async Task UpdateMemberProfileAsync(Guid userId, MemberProfileViewModel model)
    {
        var profile = await _profileRepo.GetTableAsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            profile = new UserProfile 
            { 
                UserId = userId,
                FullName = model.FullName
            };
            await _profileRepo.AddAsync(profile);
            await _profileRepo.SaveChangesAsync(); 
        }

        profile.FullName = model.FullName;
        profile.ContactEmail = model.Email;
        profile.Phone = model.Phone;
        profile.Location = model.Location;
        profile.ProfessionalSummary = model.ProfessionalSummary;
        profile.GitHubUrl = model.GitHubUrl;
        profile.LinkedInUrl = model.LinkedInUrl;

        await _profileRepo.SaveChangesAsync();

        var profileId = profile.Id;

        var exps = await _expRepo.GetTableNoTracking().Where(e => e.UserProfileId == profileId).ToListAsync();
        if (exps.Any()) await _expRepo.DeleteRangeAsync(exps);

        var edus = await _eduRepo.GetTableNoTracking().Where(e => e.UserProfileId == profileId).ToListAsync();
        if (edus.Any()) await _eduRepo.DeleteRangeAsync(edus);

        var projs = await _projRepo.GetTableNoTracking().Where(p => p.UserProfileId == profileId).ToListAsync();
        if (projs.Any()) await _projRepo.DeleteRangeAsync(projs);

        var skills = await _skillRepo.GetTableNoTracking().Where(s => s.UserProfileId == profileId).ToListAsync();
        if (skills.Any()) await _skillRepo.DeleteRangeAsync(skills);

        if (model.Experiences != null)
        {
            var newExps = model.Experiences
                .Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Company))
                .Select(e => new Experience { UserProfileId = profileId, Title = e.Title, Company = e.Company, Description = e.Description, StartDate = DateTime.UtcNow })
                .ToList();
            if (newExps.Any()) await _expRepo.AddRangeAsync(newExps);
        }

        if (model.Educations != null)
        {
            var newEdus = model.Educations
                .Where(e => !string.IsNullOrWhiteSpace(e.Degree) && !string.IsNullOrWhiteSpace(e.University))
                .Select(e => new Education { UserProfileId = profileId, Degree = e.Degree, University = e.University, StartDate = DateTime.UtcNow })
                .ToList();
            if (newEdus.Any()) await _eduRepo.AddRangeAsync(newEdus);
        }

        if (model.Projects != null)
        {
            var newProjs = model.Projects
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => new Project { UserProfileId = profileId, Name = p.Name, Description = p.Description, Period = p.Period, Url = p.Url })
                .ToList();
            if (newProjs.Any()) await _projRepo.AddRangeAsync(newProjs);
        }

        var newSkills = new List<UserSkill>();
        if (model.TechnicalSkills != null)
        {
            newSkills.AddRange(model.TechnicalSkills
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new UserSkill { UserProfileId = profileId, Name = s, Type = GDG_DashBoard.DAL.Eums.SkillType.Technical }));
        }
        if (model.SoftSkills != null)
        {
            newSkills.AddRange(model.SoftSkills
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new UserSkill { UserProfileId = profileId, Name = s, Type = GDG_DashBoard.DAL.Eums.SkillType.SoftSkill }));
        }
        if (newSkills.Any()) await _skillRepo.AddRangeAsync(newSkills);

        await _profileRepo.SaveChangesAsync();
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
        // Find any Resource of type Quiz whose URL contains this quiz's ID
        var marker = $"/Quiz/TakeQuiz/{quizId}";
        var quizResource = await _resourceRepo.GetTableNoTracking()
            .Include(r => r.RoadmapLevel)
            .FirstOrDefaultAsync(r =>
                r.Type == GDG_DashBoard.DAL.Eums.ResourceType.Quiz &&
                r.Url.Contains(marker));

        if (quizResource == null) return; // quiz not used as a resource, nothing to do

        // Mark that resource as completed (only if not already done)
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
            return; // already marked, nothing to recalculate
        }
        await _resourceProgressRepo.SaveChangesAsync();

        // Now delegate the full progress recalculation to ToggleResourceProgressAsync
        // We re-use it but it will toggle — since we just set IsCompleted = true,
        // calling it again would flip it. Instead recalculate inline:
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
}
