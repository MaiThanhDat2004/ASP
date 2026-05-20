using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskOutputRequirement
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }

        [Required]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Định dạng tệp tin")]
        public string? AllowedFileFormat { get; set; }

        [Display(Name = "Thời hạn nộp")]
        public DateTime? Deadline { get; set; }

        // Deadline gốc trước khi gia hạn
        [Display(Name = "Deadline gốc")]
        public DateTime? OriginalDeadline { get; set; }

        // Số lần gia hạn
        [Display(Name = "Số lần gia hạn")]
        public int ExtendCount { get; set; } = 0;

        // Trạng thái không đạt tiến độ
        [Display(Name = "Không đạt tiến độ")]
        public bool IsOverdue { get; set; } = false;

        [Display(Name = "Ngày đánh không đạt")]
        public DateTime? OverdueMarkedAt { get; set; }

        [Display(Name = "Lý do không đạt")]
        public string? OverdueReason { get; set; }

        // Người thực hiện chính (bắt buộc)
        [Display(Name = "Người thực hiện chính")]
        public int? PrimaryAssigneeId { get; set; }

        // Người hỗ trợ (lưu dạng "1,2,3")
        [Display(Name = "Người hỗ trợ")]
        public string? SupportAssigneeIds { get; set; }

        [Display(Name = "Bắt buộc")]
        public bool IsRequired { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        // Navigation
        public virtual TaskItem Task { get; set; }
        public virtual User? PrimaryAssignee { get; set; }
        public virtual ICollection<TaskOutputSubmission> Submissions { get; set; } = new List<TaskOutputSubmission>();
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}