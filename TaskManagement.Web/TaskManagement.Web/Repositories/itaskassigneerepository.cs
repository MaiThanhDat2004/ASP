using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface ITaskAssigneeRepository
    {
        IEnumerable<TaskAssignee> GetByTaskId(int taskId);      // ai được giao task này
        IEnumerable<TaskAssignee> GetByUserId(int userId);      // task nào được giao cho user này
        bool IsAssigned(int taskId, int userId);                // kiểm tra user có được giao task không
        void Add(TaskAssignee assignee);
        void Remove(int taskId, int userId);                    // thu hồi giao việc
        void Save();
    }
}