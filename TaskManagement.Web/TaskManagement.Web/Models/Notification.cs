using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        // Người nhận thông báo
        public int UserId { get; set; }

        // Nội dung
        public string Message { get; set; }

        // Loại: "task_assigned", "submission_uploaded", "submission_reviewed"
        public string Type { get; set; }

        // Link dẫn đến trang liên quan
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}