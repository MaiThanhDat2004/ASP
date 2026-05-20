using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly AppDbContext _context;

        public SubmissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TaskSubmission> GetByTaskId(int taskId)
        {
            return _context.TaskSubmissions
                .Where(s => s.TaskId == taskId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();
        }

        public TaskSubmission GetById(int submissionId)
        {
            return _context.TaskSubmissions.Find(submissionId);
        }

        public void Add(TaskSubmission submission)
        {
            _context.TaskSubmissions.Add(submission);
        }

        public void Update(TaskSubmission submission)
        {
            _context.TaskSubmissions.Update(submission);
        }

        public IEnumerable<TaskSubmission> GetByUserId(int userId)
        {
            return _context.TaskSubmissions
                .Where(s => s.SubmittedBy == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}