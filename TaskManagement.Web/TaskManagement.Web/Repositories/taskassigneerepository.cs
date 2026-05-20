using Microsoft.EntityFrameworkCore;
using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class TaskAssigneeRepository : ITaskAssigneeRepository
    {
        private readonly AppDbContext _context;

        public TaskAssigneeRepository(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách người được giao cho task
        public IEnumerable<TaskAssignee> GetByTaskId(int taskId)
        {
            return _context.TaskAssignees
                .Include(ta => ta.User)
                .Where(ta => ta.TaskId == taskId)
                .ToList();
        }

        // Lấy danh sách task được giao cho user
        public IEnumerable<TaskAssignee> GetByUserId(int userId)
        {
            return _context.TaskAssignees
                .Include(ta => ta.Task)
                .Where(ta => ta.UserId == userId)
                .ToList();
        }

        // Kiểm tra user có được giao task không
        public bool IsAssigned(int taskId, int userId)
        {
            return _context.TaskAssignees
                .Any(ta => ta.TaskId == taskId && ta.UserId == userId);
        }

        public void Add(TaskAssignee assignee)
        {
            _context.TaskAssignees.Add(assignee);
        }

        // Thu hồi giao việc
        public void Remove(int taskId, int userId)
        {
            var assignee = _context.TaskAssignees
                .FirstOrDefault(ta => ta.TaskId == taskId && ta.UserId == userId);
            if (assignee != null)
                _context.TaskAssignees.Remove(assignee);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}