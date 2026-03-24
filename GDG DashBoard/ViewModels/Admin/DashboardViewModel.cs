using GDG_DashBoard.BLL.Dtos.Admin;

namespace GDG_DashBoard.ViewModels.Admin;

public class DashboardViewModel
{
    public int TotalMembers { get; set; }
    public int ActiveThisWeek { get; set; }
    public int TotalRoadmaps { get; set; }
    public int PendingReviewRoadmaps { get; set; }

    public IList<MemberOverviewDto> Members { get; set; } = new List<MemberOverviewDto>();
    public IList<MemberOverviewDto> TopContributors { get; set; } = new List<MemberOverviewDto>();

    // Computed for the progress bar animation
    public int ActivePercentage =>
        TotalMembers > 0 ? (int)Math.Round((double)ActiveThisWeek / TotalMembers * 100) : 0;
}
