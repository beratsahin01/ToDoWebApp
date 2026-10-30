using System.ComponentModel.DataAnnotations;
using ToDoWebApp.Models.Enums;

namespace ToDoWebApp.Models.DTOs
{
    public class WorkGroupShareDto
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public GroupRole Role { get; set; }
    }
}
