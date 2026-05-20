using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên tài liệu")]
        [Display(Name = "Tên tài liệu")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // Tên file gốc
        [Display(Name = "Tên file")]
        public string FileName { get; set; }

        // Đường dẫn lưu trên server
        public string FilePath { get; set; }

        // Kích thước file (bytes)
        public long FileSize { get; set; }

        // Người upload
        public int UploadedBy { get; set; }

        [Display(Name = "Ngày upload")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Phiên bản (mỗi lần upload lại cùng title thì tăng lên)
        public int Version { get; set; } = 1;

        // GroupId để nhóm các phiên bản của cùng 1 tài liệu
        public int? GroupId { get; set; }
    }
}