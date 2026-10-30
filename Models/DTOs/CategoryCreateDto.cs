using System.ComponentModel.DataAnnotations;

namespace ToDoWebApp.Models.DTOs
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Kategori adı boş olamaz.")]
        public string Name { get; set; } = string.Empty;
        
        public string? ColorClass { get; set; } 

        public int? WorkGroupId { get; set; }
    }
}
