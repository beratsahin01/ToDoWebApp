using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using ToDoWebApp.Models; 
using ToDoWebApp.Models.Data;
using Microsoft.AspNetCore.Identity;
using ToDoWebApp.Services;

namespace ToDoWebApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IEmailService emailService, ILogger<AuthController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Lütfen e-posta ve şifrenizi giriniz.";
                return View();
            }

            var cleanInput = email.Trim();
            var cleanInputLower = cleanInput.ToLowerInvariant();

            // E-posta veya kullanıcı adına göre kullanıcıyı bul
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Email != null && (u.Email == cleanInput || u.Email.ToLower() == cleanInputLower)) ||
                (u.Username != null && (u.Username == cleanInput || u.Username.ToLower() == cleanInputLower)));

            if (user != null)
            {
                var passwordHasher = new PasswordHasher<User>();
                bool isPasswordValid = false;

                // 1. Önce hash doğrulamasını dene (düz metin eski şifrelerde FormatException fırlatılmasını engelle)
                try
                {
                    var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);
                    if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        isPasswordValid = true;
                    }
                }
                catch
                {
                    isPasswordValid = false;
                }

                // 2. Düz metin şifre kontrolü ve otomatik olarak güvenli hash'e dönüştürülmesi
                if (!isPasswordValid && user.Password == password)
                {
                    user.Password = passwordHasher.HashPassword(user, password);
                    await _context.SaveChangesAsync();
                    isPasswordValid = true;
                }

                if (isPasswordValid)
                {
                    string safeUsername = !string.IsNullOrWhiteSpace(user.Username)
                        ? user.Username
                        : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email.Split('@')[0] : "Kullanıcı");

                    string safeEmail = user.Email ?? string.Empty;

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, safeEmail),
                        new Claim(ClaimTypes.Name, safeUsername)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    _logger.LogInformation("Kullanıcı giriş yaptı: {UserId}", user.Id);
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Geçersiz e-posta veya şifre.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Lütfen formu eksiksiz doldurunuz.";
                return View(model);
            }

            var cleanUsername = model.Username.Trim();
            var cleanEmail = model.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == cleanEmail))
            {
                ViewBag.Error = "Bu e-posta adresi zaten kullanılıyor.";
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username != null && u.Username.ToLower() == cleanUsername.ToLower()))
            {
                ViewBag.Error = "Bu kullanıcı adı zaten kullanılıyor.";
                return View(model);
            }

            var passwordHasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Username = cleanUsername,
                Email = cleanEmail
            };
            newUser.Password = passwordHasher.HashPassword(newUser, model.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Yeni kullanıcı kaydı oluşturuldu: {Email}", cleanEmail);
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ViewBag.Error = "Lütfen geçerli bir e-posta adresi girin.";
                return View();
            }

            var cleanEmail = Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == cleanEmail);

            if (user != null)
            {
                var tokenBytes = RandomNumberGenerator.GetBytes(32);
                user.ResetToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
                user.ResetTokenExpire = DateTime.UtcNow.AddHours(1);

                await _context.SaveChangesAsync();

                string? resetLink = Url.Action("ResetPassword", "Auth", new { token = user.ResetToken }, protocol: HttpContext.Request.Scheme);
                
                if (!string.IsNullOrEmpty(resetLink))
                {
                    await _emailService.SendPasswordResetEmailAsync(cleanEmail, resetLink);
                }
            }

            ViewBag.Success = "Eğer sistemimizde böyle bir hesap varsa, şifre sıfırlama bağlantısı gönderilmiştir.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string Token, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor.";
                return View();
            }

            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
            {
                ViewBag.Error = "Şifre en az 6 karakter olmalıdır.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == Token && u.ResetTokenExpire > DateTime.UtcNow);

            if (user == null)
            {
                ViewBag.Error = "Geçersiz veya süresi dolmuş bir şifre sıfırlama bağlantısı kullandınız!";
                return View();
            }

            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, NewPassword);

            user.ResetToken = null;
            user.ResetTokenExpire = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Kullanıcı şifresi sıfırlandı: {UserId}", user.Id);
            ViewBag.Success = "Şifreniz başarıyla güncellendi! Artık giriş yapabilirsiniz.";
            return View();
        }
    }
}
