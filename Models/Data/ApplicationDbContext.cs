using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;



namespace ToDoWebApp.Models.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems
        {
            get; set;

        }
        public DbSet<User> Users
        {
            get; set;
        }
        public DbSet<Category> Categories
        {
            get; set;
        }
        
        public DbSet<WorkGroup> WorkGroups { get; set; }
        public DbSet<WorkGroupMember> WorkGroupMembers { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Priority)
                .HasConversion<string>();

            modelBuilder.Entity<WorkGroupMember>()
                .Property(wm => wm.Role)
                .HasConversion<string>();

            modelBuilder.Entity<WorkGroupMember>()
                .HasOne(wm => wm.WorkGroup)
                .WithMany(wg => wg.Members)
                .HasForeignKey(wm => wm.WorkGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkGroupMember>()
                .HasOne(wm => wm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.NoAction);
                
            modelBuilder.Entity<WorkGroup>()
                .HasOne(wg => wg.Owner)
                .WithMany()
                .HasForeignKey(wg => wg.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Friendship relations
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Comment relations (prevent cascade cycle)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
