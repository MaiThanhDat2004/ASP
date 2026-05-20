using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Web.Repositories;

namespace TaskManagement.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationsController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        // API: lấy danh sách thông báo (trả JSON cho dropdown)
        [HttpGet]
        public IActionResult GetNotifications()
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var notifications = _notificationRepository.GetByUserId(userId).ToList();
            var unreadCount = _notificationRepository.CountUnread(userId);

            var result = notifications.Select(n => new {
                n.NotificationId,
                n.Message,
                n.Type,
                n.Link,
                n.IsRead,
                CreatedAt = n.CreatedAt.ToString("HH:mm dd/MM/yyyy")
            });

            return Json(new { unreadCount, notifications = result });
        }

        // Đánh dấu tất cả đã đọc
        [HttpPost]
        public IActionResult MarkAllRead()
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            _notificationRepository.MarkAllAsRead(userId);
            _notificationRepository.Save();
            return Ok();
        }

        // Đánh dấu 1 thông báo đã đọc và redirect
        public IActionResult Read(int id)
        {
            _notificationRepository.MarkAsRead(id);
            _notificationRepository.Save();

            var n = _notificationRepository.GetByUserId(
                int.Parse(User.FindFirst("UserId")!.Value))
                .FirstOrDefault(x => x.NotificationId == id);

            if (!string.IsNullOrWhiteSpace(n?.Link))
                return Redirect(n.Link);

            return RedirectToAction("Index", "Tasks");
        }
    }
}