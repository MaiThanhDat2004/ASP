using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskComment
    {
        [Key]
        public int CommentId { get; set; }

        // Chat chung của task (nullable)
        public int? TaskId { get; set; }

        // Chat riêng của output (nullable)
        public int? OutputRequirementId { get; set; }

        // Người gửi
        public int SenderId { get; set; }

        [Required]
        [Display(Name = "Nội dung")]
        public string Message { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual TaskItem? Task { get; set; }
        public virtual TaskOutputRequirement? OutputRequirement { get; set; }
        public virtual User Sender { get; set; }
    }
}