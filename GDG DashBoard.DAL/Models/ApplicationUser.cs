using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDG_DashBoard.DAL.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? Season { get; set; }
        public bool IsActive { get; set; } = true;

        public UserProfile? UserProfile { get; set; }
        public ICollection<UserEnrollment> Enrollments { get; set; } = new List<UserEnrollment>();
        public ICollection<UserNodeProgress> Progresses { get; set; } = new List<UserNodeProgress>();
        public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public ICollection<CommunityGroup> InstructedGroups { get; set; } = new List<CommunityGroup>();
    }
}
