using GDG_DashBoard.BLL.Dtos.Member;
using GDG_DashBoard.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GDG_DashBoard.DAL.Repositores.GenericRepository;

namespace GDG_DashBoard.BLL.Services.Member;

public class MemberDashboardService : IMemberDashboardService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGenericRepositoryAsync<UserProfile> _profileRepo;
    private readonly IGenericRepositoryAsync<GroupMember> _groupMemberRepo;
    private readonly IGenericRepositoryAsync<UserEnrollment> _enrollmentRepo;

    public MemberDashboardService(
        UserManager<ApplicationUser> userManager,
        IGenericRepositoryAsync<UserProfile> profileRepo,
        IGenericRepositoryAsync<GroupMember> groupMemberRepo,
        IGenericRepositoryAsync<UserEnrollment> enrollmentRepo)
    {
        _userManager = userManager;
        _profileRepo = profileRepo;
        _groupMemberRepo = groupMemberRepo;
        _enrollmentRepo = enrollmentRepo;
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
}
