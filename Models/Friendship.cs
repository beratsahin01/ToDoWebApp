namespace ToDoWebApp.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public int AddresseeId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public virtual User Requester { get; set; } = null!;
        public virtual User Addressee { get; set; } = null!;
    }
}
