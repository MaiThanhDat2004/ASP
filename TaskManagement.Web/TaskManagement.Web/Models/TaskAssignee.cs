using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskAssignee
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }

        // Người được giao
        public int UserId { get; set; }

        // Ai giao
        public int AssignedBy { get; set; }

        [Display(Name = "Ngày giao")]
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual TaskItem Task { get; set; }
        public virtual User User { get; set; }
    }
}