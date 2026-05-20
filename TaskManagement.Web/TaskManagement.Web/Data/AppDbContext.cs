using Microsoft.EntityFrameworkCore;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }
        public DbSet<TaskOutputRequirement> TaskOutputRequirements { get; set; }
        public DbSet<TaskOutputSubmission> TaskOutputSubmissions { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // MỚI
        public DbSet<TaskComment> TaskComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // GroupMember unique
            modelBuilder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.GroupId, gm.UserId })
                .IsUnique();

            // TaskAssignee unique
            modelBuilder.Entity<TaskAssignee>()
                .HasIndex(ta => new { ta.TaskId, ta.UserId })
                .IsUnique();

            // TaskItem → Group
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Group)
                .WithMany(g => g.Tasks)
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // GroupMember → User
            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMembers)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // GroupMember → Group
            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskAssignee → TaskItem
            modelBuilder.Entity<TaskAssignee>()
                .HasOne(ta => ta.Task)
                .WithMany(t => t.Assignees)
                .HasForeignKey(ta => ta.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskAssignee → User
            modelBuilder.Entity<TaskAssignee>()
                .HasOne(ta => ta.User)
                .WithMany()
                .HasForeignKey(ta => ta.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TaskSubmission → TaskItem
            modelBuilder.Entity<TaskSubmission>()
                .HasOne(ts => ts.Task)
                .WithMany(t => t.Submissions)
                .HasForeignKey(ts => ts.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskOutputRequirement → TaskItem
            modelBuilder.Entity<TaskOutputRequirement>()
                .HasOne(r => r.Task)
                .WithMany()
                .HasForeignKey(r => r.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskOutputRequirement → PrimaryAssignee (User)
            modelBuilder.Entity<TaskOutputRequirement>()
                .HasOne(r => r.PrimaryAssignee)
                .WithMany()
                .HasForeignKey(r => r.PrimaryAssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            // TaskOutputSubmission → TaskOutputRequirement
            modelBuilder.Entity<TaskOutputSubmission>()
                .HasOne(s => s.Requirement)
                .WithMany(r => r.Submissions)
                .HasForeignKey(s => s.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskComment → TaskItem (chat chung)
            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.Task)
                .WithMany()
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // TaskComment → TaskOutputRequirement (chat output)
            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.OutputRequirement)
                .WithMany(r => r.Comments)
                .HasForeignKey(c => c.OutputRequirementId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // TaskComment → User (sender)
            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}