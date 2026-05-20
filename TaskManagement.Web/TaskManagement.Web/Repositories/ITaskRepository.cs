using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface ITaskRepository
    {
        IEnumerable<TaskItem> GetAll();

        TaskItem GetById(int id);

        void Add(TaskItem task);

        void Update(TaskItem task);

        void Delete(int id);

        void Save();
    }
}