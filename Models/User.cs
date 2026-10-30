namespace ToDoWebApp.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public  virtual  List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        public string? ResetToken { get; set; } 
        public DateTime? ResetTokenExpire { get; set; }
        
        public string? ProfilePictureUrl { get; set; }
        
        public virtual List<WorkGroupMember> GroupMemberships { get; set; } = new List<WorkGroupMember>();
    }
}
