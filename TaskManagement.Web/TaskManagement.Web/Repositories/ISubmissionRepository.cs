using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface ISubmissionRepository
    {
        // Lấy tất cả bài nộp của một task
        IEnumerable<TaskSubmission> GetByTaskId(int taskId);

        // Lấy một bài nộp theo id
        TaskSubmission GetById(int submissionId);

        // Thêm bài nộp mới
        void Add(TaskSubmission submission);

        // Cập nhật (dùng khi Admin đánh giá)
        void Update(TaskSubmission submission);

        // Lấy tất cả bài nộp của một user
        IEnumerable<TaskSubmission> GetByUserId(int userId);

        void Save();
    }
}