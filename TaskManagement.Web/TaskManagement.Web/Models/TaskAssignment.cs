using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Web.Models
{
    public class TaskAssignment
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }

        public int UserId { get; set; }

        public DateTime AssignedAt { get; set; }
    }
}