using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface ITaskOutputRepository
    {
        // Requirements
        IEnumerable<TaskOutputRequirement> GetRequirementsByTaskId(int taskId);
        TaskOutputRequirement? GetRequirementById(int id);
        void AddRequirement(TaskOutputRequirement req);
        void UpdateRequirement(TaskOutputRequirement req);
        void DeleteRequirement(int id);

        // Submissions
        IEnumerable<TaskOutputSubmission> GetSubmissionsByRequirementId(int requirementId);
        TaskOutputSubmission? GetSubmissionById(int id);
        void AddSubmission(TaskOutputSubmission sub);
        void UpdateSubmission(TaskOutputSubmission sub);

        // Kiểm tra task đã hoàn thành tất cả output chưa
        bool IsAllOutputsApproved(int taskId);

        void Save();
    }
}