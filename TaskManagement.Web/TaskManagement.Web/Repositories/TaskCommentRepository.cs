using Microsoft.EntityFrameworkCore;
using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class TaskCommentRepository : ITaskCommentRepository
    {
        private readonly AppDbContext _context;

        public TaskCommentRepository(AppDbContext context)
        {
            _context = context;
        }

        // Lấy chat chung của task (OutputRequirementId == null)
        public IEnumerable<TaskComment> GetByTaskId(int taskId)
        {
            return _context.TaskComments
                .Include(c => c.Sender)
                .Where(c => c.TaskId == taskId && c.OutputRequirementId == null)
                .OrderBy(c => c.SentAt)
                .ToList();
        }

        // Lấy chat riêng của output
        public IEnumerable<TaskComment> GetByOutputRequirementId(int requirementId)
        {
            return _context.TaskComments
                .Include(c => c.Sender)
                .Where(c => c.OutputRequirementId == requirementId)
                .OrderBy(c => c.SentAt)
                .ToList();
        }

        public void Add(TaskComment comment)
        {
            _context.TaskComments.Add(comment);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}