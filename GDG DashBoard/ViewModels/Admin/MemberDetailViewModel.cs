using GDG_DashBoard.BLL.Dtos.Admin;

namespace GDG_DashBoard.ViewModels.Admin;

public class MemberDetailViewModel
{
    public MemberDetailsDto Member { get; set; } = null!;

    // Convenience computed property for the roadmap view (code1.html)
    public EnrolledRoadmapDto? ActiveRoadmap =>
        Member.EnrolledRoadmaps.FirstOrDefault(r => r.Status == "InProgress")
        ?? Member.EnrolledRoadmaps.FirstOrDefault();
}
