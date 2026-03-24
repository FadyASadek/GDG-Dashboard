using GDG_DashBoard.BLL.Dtos.Admin;
using GDG_DashBoard.BLL.Dtos.Auth;
using GDG_DashBoard.BLL.Services.Email;
using GDG_DashBoard.DAL.Eums;
using GDG_DashBoard.DAL.Models;
using GDG_DashBoard.DAL.Repositores.GenericRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace GDG_DashBoard.BLL.Services.Admin;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    // ── Repository Injections (strict 3-tier — NO AppDbContext in BLL) ──────
    private readonly IGenericRepositoryAsync<UserProfile> _profileRepo;
    private readonly IGenericRepositoryAsync<UserEnrollment> _enrollmentRepo;
    private readonly IGenericRepositoryAsync<UserNodeProgress> _progressRepo;
    private readonly IGenericRepositoryAsync<Roadmap> _roadmapRepo;

    public AdminService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        IGenericRepositoryAsync<UserProfile> profileRepo,
        IGenericRepositoryAsync<UserEnrollment> enrollmentRepo,
        IGenericRepositoryAsync<UserNodeProgress> progressRepo,
        IGenericRepositoryAsync<Roadmap> roadmapRepo)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _profileRepo = profileRepo;
        _enrollmentRepo = enrollmentRepo;
        _progressRepo = progressRepo;
        _roadmapRepo = roadmapRepo;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. ONBOARD MEMBER
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<AuthResultDto> OnboardMemberAsync(OnboardMemberDto dto)
    {
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return AuthResultDto.Fail($"Role '{dto.Role}' does not exist.");

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return AuthResultDto.Fail($"A user with email '{dto.Email}' already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            Season = dto.Season,
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return AuthResultDto.Fail($"Failed to create user: {errors}");
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        var profile = new UserProfile
        {
            UserId = user.Id,
            FullName = dto.FullName,
            IsVerified = false
        };
        await _profileRepo.AddAsync(profile);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = _linkGenerator.GetUriByAction(
            _httpContextAccessor.HttpContext!,
            action: "SetPassword",
            controller: "Auth",
            values: new { email = dto.Email, token = token }
        );

        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #4285f4;'>Welcome to GDG Dashboard, {dto.FullName}!</h2>
                <p>An account has been created for you with the role of <strong>{dto.Role}</strong>.</p>
                <p>Please click the button below to set up your password and access the dashboard:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #4285f4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Set Your Password</a>
                </div>
                <p style='color: #666; font-size: 14px;'>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='color: #666; font-size: 12px; word-break: break-all;'><a href='{resetLink}'>{resetLink}</a></p>
                <br/>
                <p>Welcome aboard!<br/>The GDG Team</p>
            </div>
        ";

        try
        {
            await _emailService.SendEmailAsync(dto.Email, "Welcome to GDG Dashboard - Set Your Password", emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
        }

        return new AuthResultDto
        {
            Succeeded = true,
            PasswordResetToken = token
        };
    }

  
    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
    {
        var users = await _userManager.Users.ToListAsync();

        var profiles = await _profileRepo.GetTableNoTracking()
            .Select(p => new { p.UserId, p.FullName, p.IsVerified })
            .ToListAsync();

        var enrollments = await _enrollmentRepo.GetTableNoTracking()
            .Include(e => e.Roadmap)
            .ToListAsync();

        var roadmapsCount = await _roadmapRepo.GetTableNoTracking().CountAsync();
        var profileDict = profiles.ToDictionary(p => p.UserId);

        var memberOverviews = new List<MemberOverviewDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            profileDict.TryGetValue(user.Id, out var profile);

            var userEnrollments = enrollments.Where(e => e.UserId == user.Id).ToList();
            var avgProgress = userEnrollments.Any()
                ? userEnrollments.Average(e => (double)e.ProgressPercentage)
                : 0;

            var latestEnrollment = userEnrollments
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            memberOverviews.Add(new MemberOverviewDto
            {
                UserId = user.Id,
                FullName = profile?.FullName ?? user.Email ?? "Unknown",
                Email = user.Email ?? string.Empty,
                Season = user.Season,
                IsActive = user.IsActive,
                IsVerified = profile?.IsVerified ?? false,
                Role = roles.FirstOrDefault(),
                EnrolledRoadmapsCount = userEnrollments.Count,
                OverallProgressPercentage = (decimal)Math.Round(avgProgress, 2),
                EnrollmentStatus = latestEnrollment?.Status.ToString() ?? "Not Enrolled"
            });
        }

        return new DashboardOverviewDto
        {
            TotalMembers = users.Count,
            ActiveThisWeek = enrollments
                .Where(e => e.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
                .Select(e => e.UserId)
                .Distinct()
                .Count(),
            TotalRoadmaps = roadmapsCount,
            PendingReviewRoadmaps = 0,
            Members = memberOverviews.Take(10).ToList(),
            TopContributors = memberOverviews
                .OrderByDescending(m => m.OverallProgressPercentage)
                .Take(5)
                .ToList()
        };
    }
    public async Task<MemberDetailsDto?> GetMemberDetailsAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        var profile = await _profileRepo.GetTableNoTracking()
            .Include(p => p.Educations)
            .Include(p => p.Experiences)
            .Include(p => p.Projects)
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var enrollments = await _enrollmentRepo.GetTableNoTracking()
            .Include(e => e.Roadmap)
                .ThenInclude(r => r.Levels)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var completedLevelIds = await _progressRepo.GetTableNoTracking()
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => p.RoadmapLevelId)
            .ToListAsync();

        return new MemberDetailsDto
        {
            UserId = user.Id,
            FullName = profile?.FullName ?? user.Email ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Season = user.Season,
            Role = roles.FirstOrDefault(),
            IsActive = user.IsActive,
            IsVerified = profile?.IsVerified ?? false,
            ProfessionalSummary = profile?.ProfessionalSummary,
            Location = profile?.Location,
            GitHubUrl = profile?.GitHubUrl,
            LinkedInUrl = profile?.LinkedInUrl,
            ResumeFileUrl = profile?.ResumeFileUrl,

            Educations = profile?.Educations
                .Select(e => new EducationDto
                {
                    Degree = e.Degree,
                    University = e.University,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                }).ToList() ?? new List<EducationDto>(),

            Experiences = profile?.Experiences
                .Select(e => new ExperienceDto
                {
                    Title = e.Title,
                    Company = e.Company,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                }).ToList() ?? new List<ExperienceDto>(),

            Projects = profile?.Projects
                .Select(p => new ProjectDto
                {
                    Name = p.Name,
                    Description = p.Description,
                    Url = p.Url
                }).ToList() ?? new List<ProjectDto>(),

            TechnicalSkills = profile?.Skills
                .Where(s => s.Type == SkillType.Technical)
                .Select(s => new SkillDto { Name = s.Name, Type = s.Type.ToString() })
                .ToList() ?? new List<SkillDto>(),

            SoftSkills = profile?.Skills
                .Where(s => s.Type == SkillType.SoftSkill)
                .Select(s => new SkillDto { Name = s.Name, Type = s.Type.ToString() })
                .ToList() ?? new List<SkillDto>(),

            EnrolledRoadmaps = enrollments.Select(e => new EnrolledRoadmapDto
            {
                RoadmapId = e.RoadmapId,
                RoadmapTitle = e.Roadmap.Title,
                DifficultyLevel = e.Roadmap.Level.ToString(),
                ProgressPercentage = e.ProgressPercentage,
                Status = e.Status.ToString(),
                Levels = e.Roadmap.Levels
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => new RoadmapLevelProgressDto
                    {
                        LevelId = l.Id,
                        Title = l.Title,
                        Instructions = l.Instructions,
                        OrderIndex = l.OrderIndex,
                        IsCompleted = completedLevelIds.Contains(l.Id)
                    }).ToList()
            }).ToList()
        };
    }

    public async Task<List<MemberOverviewDto>> GetAllMembersAsync(string? searchString)
    {
        var usersQuery = _userManager.Users.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim().ToLower();
            usersQuery = usersQuery.Where(u => 
                (u.UserName != null && u.UserName.ToLower().Contains(search)) || 
                (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        var users = await usersQuery.ToListAsync();

        var profiles = await _profileRepo.GetTableNoTracking()
            .Select(p => new { p.UserId, p.FullName, p.IsVerified })
            .ToListAsync();

        var enrollments = await _enrollmentRepo.GetTableNoTracking()
            .Include(e => e.Roadmap)
            .ToListAsync();

        var profileDict = profiles.ToDictionary(p => p.UserId);
        var memberOverviews = new List<MemberOverviewDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            profileDict.TryGetValue(user.Id, out var profile);

            var userEnrollments = enrollments.Where(e => e.UserId == user.Id).ToList();
            var avgProgress = userEnrollments.Any()
                ? userEnrollments.Average(e => (double)e.ProgressPercentage)
                : 0;

            var latestEnrollment = userEnrollments
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            memberOverviews.Add(new MemberOverviewDto
            {
                UserId = user.Id,
                FullName = profile?.FullName ?? user.Email ?? "Unknown",
                Email = user.Email ?? string.Empty,
                Season = user.Season,
                IsActive = user.IsActive,
                IsVerified = profile?.IsVerified ?? false,
                Role = roles.FirstOrDefault(),
                EnrolledRoadmapsCount = userEnrollments.Count,
                OverallProgressPercentage = (decimal)Math.Round(avgProgress, 2),
                EnrollmentStatus = latestEnrollment?.Status.ToString() ?? "Not Enrolled"
            });
        }

        return memberOverviews.OrderBy(m => m.FullName).ToList();
    }

    public async Task<bool> ResendActivationTokenAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = _linkGenerator.GetUriByAction(
            _httpContextAccessor.HttpContext!,
            action: "SetPassword",
            controller: "Auth",
            values: new { email = user.Email, token = token }
        );

        var profile = await _profileRepo.GetTableNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        var fullName = profile?.FullName ?? user.Email;

        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #4285f4;'>GDG Dashboard - Setup Link</h2>
                <p>Hello {fullName},</p>
                <p>An administrator has requested a new setup link for your dashboard account.</p>
                <p>Please click the button below to set up your password:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #4285f4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Set Your Password</a>
                </div>
                <p style='color: #666; font-size: 14px;'>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='color: #666; font-size: 12px; word-break: break-all;'><a href='{resetLink}'>{resetLink}</a></p>
            </div>
        ";

        try
        {
            await _emailService.SendEmailAsync(user.Email!, "GDG Dashboard - New Setup Link", emailBody);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
