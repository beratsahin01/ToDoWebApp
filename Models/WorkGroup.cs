using System.Text.Json.Serialization;

namespace ToDoWebApp.Models
{
    public class WorkGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwnerId { get; set; }

        [JsonIgnore]
        public User? Owner { get; set; }

        [JsonIgnore]
        public List<WorkGroupMember> Members { get; set; } = new List<WorkGroupMember>();

        [JsonIgnore]
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        
        [JsonIgnore]
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
