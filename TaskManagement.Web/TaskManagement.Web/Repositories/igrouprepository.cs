using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface IGroupRepository
    {
        IEnumerable<Group> GetAll();
        Group GetById(int id);
        IEnumerable<Group> GetByUserId(int userId);         // nhóm user đang tham gia
        IEnumerable<Group> GetByLeaderId(int userId);       // nhóm user là Leader
        void Add(Group group);
        void Update(Group group);
        void Delete(int id);
        void Save();
    }
}