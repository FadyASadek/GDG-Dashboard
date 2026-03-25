using GDG_DashBoard.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace GDGDashBoard.DAL.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserSkill> UserSkills => Set<UserSkill>();
        public DbSet<Education> Educations => Set<Education>();
        public DbSet<Experience> Experiences => Set<Experience>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Roadmap> Roadmaps => Set<Roadmap>();
        public DbSet<RoadmapLevel> RoadmapLevels => Set<RoadmapLevel>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<UserEnrollment> UserEnrollments => Set<UserEnrollment>();
        public DbSet<UserNodeProgress> UserNodeProgresses => Set<UserNodeProgress>();
        public DbSet<CommunityGroup> CommunityGroups => Set<CommunityGroup>();
        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            builder.Entity<UserProfile>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<UserSkill>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Education>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Experience>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Roadmap>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<RoadmapLevel>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Resource>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<UserEnrollment>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<UserNodeProgress>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<CommunityGroup>().HasQueryFilter(e => !e.IsDeleted);

            // 2. Relationships Configurations
            builder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Roadmap Creator FK — must use Restrict to avoid cascade cycle
            // (ApplicationUser → UserEnrollment → Roadmap already cascades)
            builder.Entity<Roadmap>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent Cascade Delete Cycles
            builder.Entity<UserEnrollment>()
                .HasOne(ue => ue.Roadmap)
                .WithMany(r => r.Enrollments)
                .HasForeignKey(ue => ue.RoadmapId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserNodeProgress>()
                .HasOne(unp => unp.RoadmapLevel)
                .WithMany(rl => rl.UserProgresses)
                .HasForeignKey(unp => unp.RoadmapLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique Constraint: prevent duplicate enrollment
            builder.Entity<UserEnrollment>()
                .HasIndex(ue => new { ue.UserId, ue.RoadmapId })
                .IsUnique();

            // GroupMember Configurations
            builder.Entity<GroupMember>()
                .HasKey(gm => new { gm.GroupId, gm.MemberId });

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.GroupMembers)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Member)
                .WithMany(m => m.GroupMemberships)
                .HasForeignKey(gm => gm.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommunityGroup Relationships Setup
            builder.Entity<CommunityGroup>()
                .HasOne(cg => cg.Roadmap)
                .WithMany(r => r.Groups)
                .HasForeignKey(cg => cg.RoadmapId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CommunityGroup>()
                .HasOne(cg => cg.Instructor)
                .WithMany(u => u.InstructedGroups)
                .HasForeignKey(cg => cg.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Enum Conversions
            builder.Entity<UserSkill>()
                .Property(s => s.Type)
                .HasConversion<string>();

            builder.Entity<Resource>()
                .Property(r => r.Type)
                .HasConversion<string>();

            builder.Entity<UserEnrollment>()
                .Property(e => e.Status)
                .HasConversion<string>();

            builder.Entity<Roadmap>()
                .Property(r => r.Level)
                .HasConversion<string>();
            builder.Entity<UserEnrollment>()
                .Property(e => e.ProgressPercentage)
                .HasPrecision(5, 2); 
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}