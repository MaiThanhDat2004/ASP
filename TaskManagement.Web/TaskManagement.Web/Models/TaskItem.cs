using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskItem
    {
        [Key]
        public int TaskId { get; set; }

        [Display(Name = "Nhóm")]
        public int? GroupId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // "Todo" | "Doing" | "Done" | "Overdue"
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Todo";

        [Display(Name = "Mức ưu tiên")]
        public string Priority { get; set; } = "Medium";

        [Display(Name = "Hạn hoàn thành")]
        public DateTime Deadline { get; set; }

        // Ghi nhận khi bị đánh không đạt tiến độ
        [Display(Name = "Ngày đánh không đạt")]
        public DateTime? OverdueMarkedAt { get; set; }

        // Lý do không đạt tiến độ (Leader nhập)
        [Display(Name = "Lý do không đạt tiến độ")]
        public string? OverdueReason { get; set; }

        // Người đánh không đạt
        public int? OverdueMarkedBy { get; set; }

        // Deadline gốc trước khi gia hạn (để so sánh)
        [Display(Name = "Deadline gốc")]
        public DateTime? OriginalDeadline { get; set; }

        // Số lần gia hạn
        [Display(Name = "Số lần gia hạn")]
        public int ExtendCount { get; set; } = 0;

        [Display(Name = "Người tạo")]
        public int CreatedBy { get; set; }

        [Display(Name = "Người được giao")]
        public int? AssignedTo { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Group? Group { get; set; }
        public virtual ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();
        public virtual ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    }
}