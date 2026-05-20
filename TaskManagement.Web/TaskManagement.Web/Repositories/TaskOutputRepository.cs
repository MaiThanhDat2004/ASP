using Microsoft.EntityFrameworkCore;
using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class TaskOutputRepository : ITaskOutputRepository
    {
        private readonly AppDbContext _context;
        public TaskOutputRepository(AppDbContext context) => _context = context;

        public IEnumerable<TaskOutputRequirement> GetRequirementsByTaskId(int taskId) =>
            _context.TaskOutputRequirements
                .Include(r => r.Submissions)
                .Include(r => r.PrimaryAssignee)
                .Include(r => r.Comments)
                    .ThenInclude(c => c.Sender)
                .Where(r => r.TaskId == taskId)
                .OrderBy(r => r.SortOrder)
                .ToList();

        public TaskOutputRequirement? GetRequirementById(int id) =>
            _context.TaskOutputRequirements
                .Include(r => r.Submissions)
                .Include(r => r.PrimaryAssignee)
                .Include(r => r.Comments)
                    .ThenInclude(c => c.Sender)
                .FirstOrDefault(r => r.Id == id);

        public void AddRequirement(TaskOutputRequirement req) =>
            _context.TaskOutputRequirements.Add(req);

        public void UpdateRequirement(TaskOutputRequirement req) =>
            _context.TaskOutputRequirements.Update(req);

        public void DeleteRequirement(int id)
        {
            var req = _context.TaskOutputRequirements.Find(id);
            if (req != null) _context.TaskOutputRequirements.Remove(req);
        }

        public IEnumerable<TaskOutputSubmission> GetSubmissionsByRequirementId(int requirementId) =>
            _context.TaskOutputSubmissions
                .Where(s => s.RequirementId == requirementId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();

        public TaskOutputSubmission? GetSubmissionById(int id) =>
            _context.TaskOutputSubmissions.Find(id);

        public void AddSubmission(TaskOutputSubmission sub) =>
            _context.TaskOutputSubmissions.Add(sub);

        public void UpdateSubmission(TaskOutputSubmission sub) =>
            _context.TaskOutputSubmissions.Update(sub);

        public bool IsAllOutputsApproved(int taskId)
        {
            var reqs = _context.TaskOutputRequirements
                .Include(r => r.Submissions)
                .Where(r => r.TaskId == taskId && r.IsRequired)
                .ToList();

            if (!reqs.Any()) return false;

            return reqs.All(r =>
                r.Submissions.Any(s => s.Status == "Approved"));
        }

        public void Save() => _context.SaveChanges();
    }
}