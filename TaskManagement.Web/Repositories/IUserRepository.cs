using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface IUserRepository
    {
        User GetByEmail(string email);
        User GetById(int id);
        IEnumerable<User> GetAll();
        void Add(User user);
        void Save();
    }
}