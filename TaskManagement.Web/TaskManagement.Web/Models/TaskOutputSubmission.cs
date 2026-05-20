using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskOutputSubmission
    {
        [Key]
        public int Id { get; set; }

        public int RequirementId { get; set; }

        public int SubmittedBy { get; set; }

        [Display(Name = "Tên file")]
        public string FileName { get; set; }

        public string FilePath { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // "Pending" | "Approved" | "NeedsRevision"
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Nhận xét")]
        public string? ReviewComment { get; set; }

        public int? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [Display(Name = "Ngày nộp")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual TaskOutputRequirement Requirement { get; set; }
    }
}