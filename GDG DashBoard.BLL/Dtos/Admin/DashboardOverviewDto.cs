namespace GDG_DashBoard.BLL.Dtos.Admin;

public class DashboardOverviewDto
{
    public int TotalMembers { get; set; }
    public int ActiveThisWeek { get; set; }
    public int TotalRoadmaps { get; set; }
    public int PendingReviewRoadmaps { get; set; }
    public IList<MemberOverviewDto> Members { get; set; } = new List<MemberOverviewDto>();
    public IList<MemberOverviewDto> TopContributors { get; set; } = new List<MemberOverviewDto>();
}
