using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using ToDoWebApp.Models.Data;

namespace ToDoWebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [AutoValidateAntiforgeryToken]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment env, IMemoryCache memoryCache)
        {
            _context = context;
            _env = env;
            _memoryCache = memoryCache;
        }

        public class UpdateProfileDto
        {
            public string Name { get; set; } = string.Empty;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var cacheKey = $"profile_{userId}";
            if (_memoryCache.TryGetValue(cacheKey, out var cachedProfile))
            {
                return Ok(cachedProfile);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var profileData = new
            {
                name = user.Username,
                user.Email,
                user.ProfilePictureUrl
            };

            _memoryCache.Set(cacheKey, profileData, TimeSpan.FromMinutes(10));
            return Ok(profileData);
        }

        [HttpPost("update-info")]
        public async Task<IActionResult> UpdateInfo([FromBody] UpdateProfileDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2)
                return BadRequest("İsim en az 2 karakter olmalıdır.");

            user.Username = dto.Name.Trim();
            await _context.SaveChangesAsync();

            _memoryCache.Remove($"profile_{userId}");
            return Ok();
        }

        [HttpPost("set-stock-picture")]
        public async Task<IActionResult> SetStockPicture([FromBody] string pictureUrl)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(pictureUrl))
                return BadRequest("Geçersiz resim seçimi.");

            // Yalnızca izin verilen Dicebear API avatar adreslerini kabul et (Güvenlik / SSRF / XSS önlemi)
            if (!pictureUrl.StartsWith("https://api.dicebear.com/"))
                return BadRequest("Yalnızca sistemde tanımlı hazır avatarlar seçilebilir.");

            user.ProfilePictureUrl = pictureUrl;
            await _context.SaveChangesAsync();

            _memoryCache.Remove($"profile_{userId}");
            return Ok(new { url = pictureUrl });
        }

        [HttpPost("upload-picture")]
        public async Task<IActionResult> UploadPicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen bir dosya seçin.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Sadece .jpg, .jpeg, .png ve .webp formatında resimler yüklenebilir.");

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Yüklenen dosya geçerli bir resim formatı değil.");

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest("Dosya boyutu 2 MB'dan büyük olamaz.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{userId}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Eski yüklenmiş profil resmini güvenli bir şekilde sil (Path Traversal önlemi: Path.GetFileName)
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/profiles/"))
            {
                var safeOldFileName = Path.GetFileName(user.ProfilePictureUrl);
                var oldFilePath = Path.Combine(uploadsFolder, safeOldFileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    catch
                    {
                        // Eski dosya silinemese bile akışı bozma
                    }
                }
            }

            var fileUrl = $"/uploads/profiles/{uniqueFileName}";
            user.ProfilePictureUrl = fileUrl;
            await _context.SaveChangesAsync();

            _memoryCache.Remove($"profile_{userId}");
            return Ok(new { url = fileUrl });
        }
    }
}
