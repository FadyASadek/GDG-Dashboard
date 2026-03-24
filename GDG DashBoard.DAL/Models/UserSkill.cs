using GDG_DashBoard.DAL.Eums;

namespace GDG_DashBoard.DAL.Models;

public class UserSkill : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public required string Name { get; set; }
    public SkillType Type { get; set; }

    public UserProfile UserProfile { get; set; } = null!;
}