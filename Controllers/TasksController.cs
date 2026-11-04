using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToDoWebApp.Models;
using ToDoWebApp.Models.Data;
using ToDoWebApp.Models.DTOs;

namespace ToDoWebApp.Controllers
{       
    [Authorize]
    [ApiController]
    [AutoValidateAntiforgeryToken]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TasksController> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TasksController(ApplicationDbContext context, IDistributedCache cache, IMemoryCache memoryCache, ILogger<TasksController> logger)
        {
            _context = context;
            _cache = cache;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        private string GetCacheKey(int userId, int? groupId)
        {
            return groupId.HasValue ? $"tasks_group_{groupId.Value}" : $"tasks_user_{userId}";
        }

        private async Task SafeInvalidateCacheAsync(int userId, int? groupId = null)
        {
            try
            {
                await _cache.RemoveAsync(GetCacheKey(userId, null));
                if (groupId.HasValue)
                {
                    await _cache.RemoveAsync(GetCacheKey(userId, groupId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache silinirken hata oluştu (uygulama çalışmaya devam ediyor).");
            }
        }

        private async Task<bool> HasGroupAccess(int userId, int? groupId, bool requireAdmin = false)
        {
            if (groupId == null) return true; // Kişisel alan

            var group = await _context.WorkGroups
                .AsNoTracking()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return false;
            
            if (group.OwnerId == userId) return true;

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null) return false;

            if (requireAdmin && member.Role != Models.Enums.GroupRole.Admin) return false;

            return true;
        }

        [HttpGet("/api/category")]
        public async Task<IActionResult> GetCategories([FromQuery] int? groupId = null)
        {
            var userId = GetCurrentUserId();
            
            if (!await HasGroupAccess(userId, groupId)) return StatusCode(403, "Bu odaya erişim yetkiniz yok.");

            var catCacheKey = $"categories_{userId}_{groupId}";
            if (_memoryCache.TryGetValue(catCacheKey, out var cachedCats))
            {
                return Ok(cachedCats);
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.WorkGroupId == groupId && (groupId != null || c.UserId == userId))
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    ColorClass = c.ColorClass
                })
                .ToListAsync();
                
            _memoryCache.Set(catCacheKey, categories, TimeSpan.FromMinutes(5));
            return Ok(categories);
        }

        [HttpPost("/api/category")]
        public async Task<IActionResult> AddCategory([FromBody] CategoryCreateDto categoryDto)
        {
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await HasGroupAccess(userId, categoryDto.WorkGroupId, requireAdmin: true)) 
                return StatusCode(403, "Bu odada kategori oluşturma yetkiniz yok (Sadece Admin).");

            var category = new Category
            {
                UserId = userId,
                Name = categoryDto.Name.Trim(),
                ColorClass = string.IsNullOrEmpty(categoryDto.ColorClass) ? "bg-primary" : categoryDto.ColorClass,
                WorkGroupId = categoryDto.WorkGroupId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            _memoryCache.Remove($"categories_{userId}_{categoryDto.WorkGroupId}");
            await SafeInvalidateCacheAsync(userId, categoryDto.WorkGroupId);
            return Ok(category);
        }

        [HttpDelete("/api/category/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = GetCurrentUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound("Kategori bulunamadı.");

            if (!await HasGroupAccess(userId, category.WorkGroupId, requireAdmin: true))
                return StatusCode(403, "Bu odada kategori silme yetkiniz yok (Sadece Admin).");
            
            // Eğer kişisel kategoriyse sadece sahibi silebilir
            if (category.WorkGroupId == null && category.UserId != userId)
                return StatusCode(403, "Silme yetkiniz yok.");

            var workGroupId = category.WorkGroupId;
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            _memoryCache.Remove($"categories_{userId}_{workGroupId}");
            await SafeInvalidateCacheAsync(userId, workGroupId);
            return Ok();
        }

        [HttpGet("api/task")]
        public async Task<IActionResult> GetAllTasks([FromQuery] int? groupId = null)
        {
            var userId = GetCurrentUserId();
            
            if (!await HasGroupAccess(userId, groupId)) return StatusCode(403, "Bu odaya erişim yetkiniz yok.");

            var cacheKey = GetCacheKey(userId, groupId);

            // 1. Redis Cache Kontrolü
            try
            {
                var cachedTasks = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedTasks))
                {
                    return Content(cachedTasks, "application/json");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache okunamadı, veritabanına dönülüyor.");
            }

            // 2. Veritabanından Getirme
            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.WorkGroupId == groupId && (groupId != null || t.UserId == userId))
                .ToListAsync();

            // 3. Redis Cache Kaydetme
            try
            {
                var serialized = JsonSerializer.Serialize(tasks, _jsonOptions);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache yazılamadı.");
            }

            return Ok(tasks);
        }

        [HttpPost("api/task")]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto newTaskDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetCurrentUserId();

            if (!await HasGroupAccess(userId, newTaskDto.WorkGroupId, requireAdmin: true))
                return StatusCode(403, "Bu odada görev oluşturma yetkiniz yok (Sadece Admin).");

            var newTask = new TaskItem
            {
                UserId = userId,
                Title = newTaskDto.Title.Trim(),
                Description = newTaskDto.Description?.Trim(),
                CategoryId = newTaskDto.CategoryId,
                WorkGroupId = newTaskDto.WorkGroupId,
                Priority = newTaskDto.Priority,
                DueDate = newTaskDto.DueDate,
                IsDone = false,
                CreateDate = DateTime.UtcNow
            };

            _context.TaskItems.Add(newTask);
            await _context.SaveChangesAsync();
            await SafeInvalidateCacheAsync(userId, newTaskDto.WorkGroupId);
            return Ok(newTask);
        }

        [HttpPut("api/task/{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto updatedTaskDto)
        {
            if (id != updatedTaskDto.Id) return BadRequest("URL'deki ID ile uyuşmuyor.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var existingTask = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id);
            if (existingTask == null) return NotFound();

            if (!await HasGroupAccess(userId, existingTask.WorkGroupId, requireAdmin: true))
                return StatusCode(403, "Bu odada görev düzenleme yetkiniz yok.");
                
            if (existingTask.WorkGroupId == null && existingTask.UserId != userId)
                return StatusCode(403, "Düzenleme yetkiniz yok.");

            // Eğer grup değişiyorsa yeni gruba da yetkisi olmalı
            if (updatedTaskDto.WorkGroupId != existingTask.WorkGroupId)
            {
                if (!await HasGroupAccess(userId, updatedTaskDto.WorkGroupId, requireAdmin: true))
                    return StatusCode(403, "Hedef odada yetkiniz yok.");
            }

            var oldGroupId = existingTask.WorkGroupId;

            existingTask.Title = updatedTaskDto.Title.Trim();
            existingTask.Description = updatedTaskDto.Description?.Trim();
            existingTask.CategoryId = updatedTaskDto.CategoryId;
            existingTask.Priority = updatedTaskDto.Priority;
            existingTask.WorkGroupId = updatedTaskDto.WorkGroupId;
            existingTask.DueDate = updatedTaskDto.DueDate;

            await _context.SaveChangesAsync();
            await SafeInvalidateCacheAsync(userId, oldGroupId);
            if (updatedTaskDto.WorkGroupId != oldGroupId)
            {
                await SafeInvalidateCacheAsync(userId, updatedTaskDto.WorkGroupId);
            }
            return Ok(existingTask);
        }

        [HttpDelete("api/task/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();
            var taskToDelete = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id);
            
            if (taskToDelete == null) return NotFound();

            if (!await HasGroupAccess(userId, taskToDelete.WorkGroupId, requireAdmin: true))
                return StatusCode(403, "Bu odada görev silme yetkiniz yok.");
                
            if (taskToDelete.WorkGroupId == null && taskToDelete.UserId != userId)
                return StatusCode(403, "Silme yetkiniz yok.");

            var groupId = taskToDelete.WorkGroupId;
            _context.TaskItems.Remove(taskToDelete);
            await _context.SaveChangesAsync();
            await SafeInvalidateCacheAsync(userId, groupId);
            return NoContent();
        }

        [HttpPatch("api/task/{id}/toggle")]
        public async Task<IActionResult> ToggleTask(int id)
        {
            var userId = GetCurrentUserId();
            var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) return NotFound();

            if (!await HasGroupAccess(userId, task.WorkGroupId, requireAdmin: true))
                return StatusCode(403, "Bu odada görev durumu değiştirme yetkiniz yok.");
                
            if (task.WorkGroupId == null && task.UserId != userId)
                return StatusCode(403, "Yetkiniz yok.");

            task.IsDone = !task.IsDone;
            await _context.SaveChangesAsync();
            await SafeInvalidateCacheAsync(userId, task.WorkGroupId);
            return Ok(task);
        }
    }
}
