using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface INotificationRepository
    {
        // Lấy tất cả thông báo của 1 user, mới nhất trước
        IEnumerable<Notification> GetByUserId(int userId);

        // Đếm số thông báo chưa đọc
        int CountUnread(int userId);

        // Thêm thông báo mới
        void Add(Notification notification);

        // Đánh dấu 1 thông báo đã đọc
        void MarkAsRead(int notificationId);

        // Đánh dấu tất cả đã đọc
        void MarkAllAsRead(int userId);

        void Save();
    }
}