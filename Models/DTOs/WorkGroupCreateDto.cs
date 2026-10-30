using System.ComponentModel.DataAnnotations;

namespace ToDoWebApp.Models.DTOs
{
    public class WorkGroupCreateDto
    {
        [Required]
        public string Name { get; set; }
    }
}
