using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ToDoWebApp.Models.Enums;


namespace ToDoWebApp.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }

       
        [Required(ErrorMessage = "Görev başlığı boş bırakılamaz!")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public bool IsDone { get; set; } = false;

        public int? WorkGroupId { get; set; }
        public virtual WorkGroup? WorkGroup { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Orta;

        [ValidateNever]
        public int? CategoryId { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Category? Category { get; set; }


        [JsonIgnore]
        [ValidateNever]
        
        
        public User? User { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        
        [JsonIgnore]
        public virtual List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
