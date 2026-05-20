using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhóm")]
        [Display(Name = "Tên nhóm")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // Người tạo nhóm
        [Display(Name = "Người tạo")]
        public int CreatedBy { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}