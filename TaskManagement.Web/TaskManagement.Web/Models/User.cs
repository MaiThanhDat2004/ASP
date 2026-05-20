using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        // "SystemAdmin" = quản trị hệ thống, "User" = người dùng thông thường
        // Vai trò Leader/Member được quản lý ở GroupMember, không gán cứng ở đây
        [Required]
        [Display(Name = "Vai trò hệ thống")]
        public string Role { get; set; } = "User";

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    }
}