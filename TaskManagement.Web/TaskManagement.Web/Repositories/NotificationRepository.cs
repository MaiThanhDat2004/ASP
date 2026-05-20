using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Notification> GetByUserId(int userId)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(30)
                .ToList();
        }

        public int CountUnread(int userId)
        {
            return _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
        }

        public void Add(Notification notification)
        {
            _context.Notifications.Add(notification);
        }

        public void MarkAsRead(int notificationId)
        {
            var n = _context.Notifications.Find(notificationId);
            if (n != null) n.IsRead = true;
        }

        public void MarkAllAsRead(int userId)
        {
            var unread = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToList();
            foreach (var n in unread) n.IsRead = true;
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}