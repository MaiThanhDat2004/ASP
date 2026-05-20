using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<User> GetAll();
        User GetById(int id);
        User GetByEmail(string email);
        void Add(User user);
        void Update(User user);
        void Delete(int id);
        void Save();
    }
}