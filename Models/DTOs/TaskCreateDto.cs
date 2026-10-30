using System.ComponentModel.DataAnnotations;
using ToDoWebApp.Models.Enums;

namespace ToDoWebApp.Models.DTOs
{
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "Görev başlığı boş bırakılamaz!")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; } = string.Empty;
        
        public TaskPriority Priority { get; set; } = TaskPriority.Orta;
        
        public int? CategoryId { get; set; }

        public int? WorkGroupId { get; set; }
        
        public DateTime? DueDate { get; set; }
    }
}
