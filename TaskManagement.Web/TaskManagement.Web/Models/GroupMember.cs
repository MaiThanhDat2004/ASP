using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class GroupMember
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }

        public int UserId { get; set; }

        // "Leader" hoặc "Member"
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Member";

        [Display(Name = "Ngày tham gia")]
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User User { get; set; }
    }
}