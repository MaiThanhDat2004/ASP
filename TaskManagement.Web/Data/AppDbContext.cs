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

        public DbSet<TaskItem> Tasks { get; set; }

        public DbSet<TaskAssignment> TaskAssignments { get; set; }
    }
}