using GDG_DashBoard.DAL.Eums;

namespace GDG_DashBoard.DAL.Models;

public class UserEnrollment : BaseEntity
{
    public required Guid UserId { get; set; }
    public Guid RoadmapId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public decimal ProgressPercentage { get; set; } = 0;

    public ApplicationUser User { get; set; } = null!;
    public Roadmap Roadmap { get; set; } = null!;
}