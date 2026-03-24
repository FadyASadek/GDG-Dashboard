namespace GDG_DashBoard.BLL.Dtos.Admin;

public class MemberOverviewDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Season { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public string? Role { get; set; }

    // Aggregated progress
    public int EnrolledRoadmapsCount { get; set; }
    public decimal OverallProgressPercentage { get; set; }
    public string EnrollmentStatus { get; set; } = string.Empty;
}
