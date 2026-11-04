using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ToDoWebApp.Models;
using ToDoWebApp.Models.Data;

namespace ToDoWebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [AutoValidateAntiforgeryToken]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<bool> UserHasTaskAccess(int userId, TaskItem task)
        {
            if (task.WorkGroupId == null)
            {
                return task.UserId == userId;
            }

            return await _context.WorkGroups
                .AnyAsync(g => g.Id == task.WorkGroupId && (g.OwnerId == userId || g.Members.Any(m => m.UserId == userId)));
        }

        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) return NotFound("Görev bulunamadı.");

            if (!await UserHasTaskAccess(userId, task))
                return StatusCode(403, "Bu görevin yorumlarına erişim yetkiniz yok.");

            var comments = await _context.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.TaskItemId == taskId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    user = new
                    {
                        c.User.Id,
                        Username = !string.IsNullOrWhiteSpace(c.User.Username) ? c.User.Username : (!string.IsNullOrWhiteSpace(c.User.Email) ? c.User.Email.Split('@')[0] : "Kullanıcı"),
                        c.User.ProfilePictureUrl
                    }
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost("{taskId}")]
        public async Task<IActionResult> AddComment(int taskId, [FromBody] string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BadRequest("Yorum boş olamaz.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) return NotFound("Görev bulunamadı.");

            if (!await UserHasTaskAccess(userId, task))
                return StatusCode(403, "Bu göreve yorum yazma yetkiniz yok.");

            var comment = new Comment
            {
                TaskItemId = taskId,
                UserId = userId,
                Content = content.Trim()
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
