using Microsoft.EntityFrameworkCore;
using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class GroupRepository : IGroupRepository
    {
        private readonly AppDbContext _context;

        public GroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Group> GetAll()
        {
            return _context.Groups
                .Include(g => g.Members)
                .ToList();
        }

        public Group GetById(int id)
        {
            return _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.Tasks)
                .FirstOrDefault(g => g.GroupId == id);
        }

        // Lấy tất cả nhóm mà user đang tham gia (cả Leader lẫn Member)
        public IEnumerable<Group> GetByUserId(int userId)
        {
            return _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Include(gm => gm.Group)
                .Select(gm => gm.Group)
                .ToList();
        }

        // Lấy các nhóm mà user là Leader
        public IEnumerable<Group> GetByLeaderId(int userId)
        {
            return _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.Role == "Leader")
                .Include(gm => gm.Group)
                .Select(gm => gm.Group)
                .ToList();
        }

        public void Add(Group group)
        {
            _context.Groups.Add(group);
        }

        public void Update(Group group)
        {
            _context.Groups.Update(group);
        }

        public void Delete(int id)
        {
            var group = _context.Groups.Find(id);
            if (group != null)
                _context.Groups.Remove(group);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}