namespace ToDoWebApp.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual TaskItem TaskItem { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
