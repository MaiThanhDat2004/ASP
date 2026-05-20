using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface ITaskCommentRepository
    {
        // Chat chung của task
        IEnumerable<TaskComment> GetByTaskId(int taskId);

        // Chat riêng của output
        IEnumerable<TaskComment> GetByOutputRequirementId(int requirementId);

        void Add(TaskComment comment);
        void Save();
    }
}