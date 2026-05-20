using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        public int TaskId { get; set; }

        public int SubmittedBy { get; set; }

        [Display(Name = "Tên file")]
        public string FileName { get; set; }

        public string FilePath { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Ngày nộp")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        // Trạng thái mới: "Pending" | "NeedsRevision" | "Approved"
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Pending";

        // Giữ lại IsApproved tạm để không break Controller/View cũ
        [Display(Name = "Kết quả đánh giá")]
        public bool? IsApproved { get; set; }

        [Display(Name = "Nhận xét")]
        public string? ReviewComment { get; set; }

        // Giữ lại AdminComment tạm
        [Display(Name = "Nhận xét Admin")]
        public string? AdminComment { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedBy { get; set; }

        // Lần nộp thứ mấy
        [Display(Name = "Lần nộp")]
        public int SubmissionRound { get; set; } = 1;

        // Navigation properties
        public virtual TaskItem Task { get; set; }
    }
}