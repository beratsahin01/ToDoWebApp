using System.Text.Json.Serialization;
using ToDoWebApp.Models.Enums;

namespace ToDoWebApp.Models
{
    public class WorkGroupMember
    {
        public int Id { get; set; }
        
        public int WorkGroupId { get; set; }
        [JsonIgnore]
        public WorkGroup? WorkGroup { get; set; }
        
        public int UserId { get; set; }
        [JsonIgnore]
        public User? User { get; set; }
        
        public GroupRole Role { get; set; }
    }
}
