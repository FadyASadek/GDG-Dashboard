using GDG_DashBoard.BLL.ViewModels.Member;
using GDG_DashBoard.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GDG_DashBoard.DAL.Repositores.GenericRepository;
using GDG_DashBoard.BLL.Dtos.Member;

namespace GDG_DashBoard.BLL.Services.Member;

public class MemberProfileService : IMemberProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGenericRepositoryAsync<UserProfile> _profileRepo;
    private readonly IGenericRepositoryAsync<UserEnrollment> _enrollmentRepo;
    private readonly IGenericRepositoryAsync<UserNodeProgress> _progressRepo;
    private readonly IGenericRepositoryAsync<UserQuizAttempt> _quizAttemptRepo;
    private readonly IGenericRepositoryAsync<Experience> _expRepo;
    private readonly IGenericRepositoryAsync<Education> _eduRepo;
    private readonly IGenericRepositoryAsync<Project> _projRepo;
    private readonly IGenericRepositoryAsync<UserSkill> _skillRepo;

    public MemberProfileService(
        UserManager<ApplicationUser> userManager,
        IGenericRepositoryAsync<UserProfile> profileRepo,
        IGenericRepositoryAsync<UserEnrollment> enrollmentRepo,
        IGenericRepositoryAsync<UserNodeProgress> progressRepo,
        IGenericRepositoryAsync<UserQuizAttempt> quizAttemptRepo,
        IGenericRepositoryAsync<Experience> expRepo,
        IGenericRepositoryAsync<Education> eduRepo,
        IGenericRepositoryAsync<Project> projRepo,
        IGenericRepositoryAsync<UserSkill> skillRepo)
    {
        _userManager = userManager;
        _profileRepo = profileRepo;
        _enrollmentRepo = enrollmentRepo;
        _progressRepo = progressRepo;
        _quizAttemptRepo = quizAttemptRepo;
        _expRepo = expRepo;
        _eduRepo = eduRepo;
        _projRepo = projRepo;
        _skillRepo = skillRepo;
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
}
