using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using ToDoWebApp.Models;
using ToDoWebApp.Models.Data;
using ToDoWebApp.Models.DTOs;
using ToDoWebApp.Models.Enums;

namespace ToDoWebApp.Controllers
{
    [Authorize]
    [ApiController]
    [AutoValidateAntiforgeryToken]
    [Route("api/workgroup")]
    public class WorkGroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public WorkGroupsController(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetMyGroups()
        {
            var userId = GetCurrentUserId();
            var cacheKey = $"workgroups_{userId}";

            if (_memoryCache.TryGetValue(cacheKey, out var cachedGroups))
            {
                return Ok(cachedGroups);
            }

            var groups = await _context.WorkGroups
                .AsNoTracking()
                .Include(g => g.Members)
                .Where(g => g.OwnerId == userId || g.Members.Any(m => m.UserId == userId))
                .Select(g => new
                {
                    Id = g.Id,
                    Name = g.Name,
                    IsOwner = g.OwnerId == userId,
                    Role = g.OwnerId == userId 
                        ? "Owner" 
                        : (g.Members.FirstOrDefault(m => m.UserId == userId) != null 
                            ? g.Members.FirstOrDefault(m => m.UserId == userId)!.Role.ToString() 
                            : "Viewer")
                })
                .ToListAsync();

            _memoryCache.Set(cacheKey, groups, TimeSpan.FromMinutes(5));
            return Ok(groups);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] WorkGroupCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var group = new WorkGroup
            {
                Name = dto.Name,
                OwnerId = userId
            };

            _context.WorkGroups.Add(group);
            await _context.SaveChangesAsync();

            _memoryCache.Remove($"workgroups_{userId}");
            return Ok(group);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var userId = GetCurrentUserId();
            var group = await _context.WorkGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == userId);
            
            if (group == null) return NotFound("Oda bulunamadı veya silme yetkiniz yok (Sadece Kurucu silebilir).");

            // Odaya ait görevleri ve görev yorumlarını sil (Foreign Key çakışmasını önle)
            var tasks = await _context.TaskItems.Where(t => t.WorkGroupId == id).ToListAsync();
            if (tasks.Any())
            {
                var taskIds = tasks.Select(t => t.Id).ToList();
                var comments = await _context.Comments.Where(c => taskIds.Contains(c.TaskItemId)).ToListAsync();
                _context.Comments.RemoveRange(comments);
                _context.TaskItems.RemoveRange(tasks);
            }

            // Odaya ait kategorileri sil
            var categories = await _context.Categories.Where(c => c.WorkGroupId == id).ToListAsync();
            if (categories.Any())
            {
                _context.Categories.RemoveRange(categories);
            }

            _context.WorkGroups.Remove(group);
            await _context.SaveChangesAsync();

            _memoryCache.Remove($"workgroups_{userId}");
            return Ok(new { message = "Oda ve tüm içeriği başarıyla silindi." });
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetGroupMembers(int id)
        {
            var userId = GetCurrentUserId();
            var group = await _context.WorkGroups
                .Include(g => g.Owner)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id && (g.OwnerId == userId || g.Members.Any(m => m.UserId == userId)));

            if (group == null) return NotFound("Oda bulunamadı.");

            var list = new List<object>();

            // Kurucu üyeyi listenin başına ekle
            string ownerName = !string.IsNullOrWhiteSpace(group.Owner?.Username) 
                ? group.Owner.Username 
                : (!string.IsNullOrWhiteSpace(group.Owner?.Email) ? group.Owner.Email.Split('@')[0] : "Kurucu");

            list.Add(new
            {
                UserId = group.OwnerId,
                Username = ownerName,
                Role = "Kurucu",
                IsOwner = true
            });

            // Diğer üyeleri ekle
            foreach (var m in group.Members)
            {
                string memberName = !string.IsNullOrWhiteSpace(m.User?.Username)
                    ? m.User.Username
                    : (!string.IsNullOrWhiteSpace(m.User?.Email) ? m.User.Email.Split('@')[0] : "Üye");

                list.Add(new
                {
                    UserId = m.UserId,
                    Username = memberName,
                    Role = m.Role.ToString(),
                    IsOwner = false
                });
            }

            return Ok(list);
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> InviteMember(int id, [FromBody] WorkGroupShareDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            var userId = GetCurrentUserId();
            var group = await _context.WorkGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound("Oda bulunamadı.");
            if (group.OwnerId != userId) return StatusCode(403, "Sadece oda kurucusu davet gönderebilir.");

            var targetUser = await _context.Users.FindAsync(dto.UserId);
            if (targetUser == null) return NotFound("Kullanıcı bulunamadı.");
            if (targetUser.Id == userId) return BadRequest("Kendinizi davet edemezsiniz.");

            var existingMember = group.Members.FirstOrDefault(m => m.UserId == targetUser.Id);
            if (existingMember != null)
            {
                existingMember.Role = dto.Role;
            }
            else
            {
                group.Members.Add(new WorkGroupMember
                {
                    UserId = targetUser.Id,
                    Role = dto.Role
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}/members/{targetUserId}")]
        public async Task<IActionResult> RemoveMember(int id, int targetUserId)
        {
            var userId = GetCurrentUserId();
            var group = await _context.WorkGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound("Oda bulunamadı.");

            // Sadece kurucu çıkarabilir veya üye kendisi ayrılabilir
            if (group.OwnerId != userId && userId != targetUserId)
            {
                return StatusCode(403, "Bu işlem için yetkiniz yok.");
            }

            if (targetUserId == group.OwnerId)
            {
                return BadRequest("Oda kurucusu odadan çıkarılamaz.");
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == targetUserId);
            if (member == null) return NotFound("Üye odada bulunamadı.");

            group.Members.Remove(member);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Üye başarıyla çıkarıldı." });
        }
    }
}
