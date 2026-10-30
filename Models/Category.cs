using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;

namespace ToDoWebApp.Models
{
    public class Category
    {

        public int UserId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }

        public int? WorkGroupId { get; set; }
        [JsonIgnore]
        public WorkGroup? WorkGroup { get; set; }

        [JsonIgnore]
        public string? ColorClass { get; set; } 

      
        [JsonIgnore]
        public List<TaskItem>? Tasks { get; set; } = new List<TaskItem>();
    }
}
