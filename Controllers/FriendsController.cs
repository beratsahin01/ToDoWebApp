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
    public class FriendsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FriendsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        private static object FormatUser(User u)
        {
            string displayName = !string.IsNullOrWhiteSpace(u.Username)
                ? u.Username
                : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email.Split('@')[0] : "Kullanıcı");

            return new
            {
                u.Id,
                Username = displayName,
                u.Email,
                u.ProfilePictureUrl
            };
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Ok(new List<object>());

            var userId = GetCurrentUserId();
            var cleanQuery = query.Trim();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId && (u.Username.Contains(cleanQuery) || u.Email.Contains(cleanQuery)))
                .Take(10)
                .ToListAsync();

            var targetIds = users.Select(u => u.Id).ToList();
            var friendships = await _context.Friendships
                .AsNoTracking()
                .Where(f => (f.RequesterId == userId && targetIds.Contains(f.AddresseeId)) ||
                            (f.AddresseeId == userId && targetIds.Contains(f.RequesterId)))
                .ToListAsync();

            var result = users.Select(u =>
            {
                var f = friendships.FirstOrDefault(x =>
                    (x.RequesterId == userId && x.AddresseeId == u.Id) ||
                    (x.AddresseeId == userId && x.RequesterId == u.Id));

                string status = "None"; // None, Friend, PendingSent, PendingReceived
                int? friendshipId = f?.Id;

                if (f != null)
                {
                    if (f.Status == "Accepted") status = "Friend";
                    else if (f.Status == "Pending")
                    {
                        status = f.RequesterId == userId ? "PendingSent" : "PendingReceived";
                    }
                }

                string displayName = !string.IsNullOrWhiteSpace(u.Username)
                    ? u.Username
                    : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email.Split('@')[0] : "Kullanıcı");

                return new
                {
                    u.Id,
                    Username = displayName,
                    u.Email,
                    u.ProfilePictureUrl,
                    Status = status,
                    FriendshipId = friendshipId
                };
            });

            return Ok(result);
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendRequest(int targetUserId)
        {
            var userId = GetCurrentUserId();

            if (userId == targetUserId) return BadRequest("Kendinize istek gönderemezsiniz.");

            var existing = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == userId && f.AddresseeId == targetUserId) ||
                (f.RequesterId == targetUserId && f.AddresseeId == userId));

            if (existing != null)
            {
                if (existing.Status == "Accepted")
                {
                    return BadRequest("Bu kullanıcı ile zaten arkadaşsınız.");
                }

                if (existing.Status == "Pending")
                {
                    if (existing.RequesterId == userId)
                    {
                        return BadRequest("Bu kullanıcıya zaten beklemede olan bir istek gönderdiniz.");
                    }
                    else
                    {
                        // Karşı taraf zaten istek göndermiş; otomatik kabul et
                        existing.Status = "Accepted";
                        await _context.SaveChangesAsync();
                        return Ok(new { message = "Bu kullanıcı size zaten istek göndermişti, istek kabul edildi ve arkadaş oldunuz!", accepted = true });
                    }
                }
            }

            var friendship = new Friendship
            {
                RequesterId = userId,
                AddresseeId = targetUserId,
                Status = "Pending"
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "İstek başarıyla gönderildi.", accepted = false });
        }

        [HttpPost("accept/{friendshipId}")]
        public async Task<IActionResult> AcceptRequest(int friendshipId)
        {
            var userId = GetCurrentUserId();
            var friendship = await _context.Friendships.FindAsync(friendshipId);

            if (friendship == null || friendship.AddresseeId != userId)
                return NotFound("İstek bulunamadı veya onaylama yetkiniz yok.");

            friendship.Status = "Accepted";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arkadaşlık isteği kabul edildi." });
        }

        [HttpDelete("{friendshipId}")]
        public async Task<IActionResult> DeleteOrDecline(int friendshipId)
        {
            var userId = GetCurrentUserId();
            var friendship = await _context.Friendships.FindAsync(friendshipId);

            if (friendship == null) return NotFound("Kayıt bulunamadı.");
            if (friendship.RequesterId != userId && friendship.AddresseeId != userId)
                return Unauthorized("Bu işlemi yapma yetkiniz yok.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return Ok(new { message = "İşlem başarıyla gerçekleştirildi." });
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetCurrentUserId();

            var friendships = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .ToListAsync();

            var pendingRequests = friendships
                .Where(f => f.Status == "Pending" && f.AddresseeId == userId)
                .Select(f => new { id = f.Id, user = FormatUser(f.Requester) });

            var sentRequests = friendships
                .Where(f => f.Status == "Pending" && f.RequesterId == userId)
                .Select(f => new { id = f.Id, user = FormatUser(f.Addressee) });

            var friends = friendships
                .Where(f => f.Status == "Accepted")
                .Select(f =>
                {
                    var friendUser = f.RequesterId == userId ? f.Addressee : f.Requester;
                    return new { id = f.Id, user = FormatUser(friendUser) };
                });

            return Ok(new { pendingRequests, sentRequests, friends });
        }
    }
}
