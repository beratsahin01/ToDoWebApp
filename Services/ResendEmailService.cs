using System.Text;
using System.Text.Json;

namespace ToDoWebApp.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ResendEmailService> _logger;
        private readonly HttpClient _httpClient;

        public ResendEmailService(IConfiguration configuration, IWebHostEnvironment env, ILogger<ResendEmailService> logger)
        {
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            string? resendApiKey = _configuration["Resend:ApiKey"];
            
            var emailData = new
            {
                from = "To Do App <onboarding@resend.dev>",
                to = new[] { toEmail },
                subject = "To Do Panel - Şifre Sıfırlama",
                html = $@"
                <div style='font-family: Arial; padding: 20px;'>
                    <h2>Şifre Sıfırlama Talebi</h2>
                    <p>Yeni şifrenizi belirlemek için aşağıdaki butona tıklayın. <b>Bu bağlantı 1 saat boyunca geçerlidir.</b></p>
                    <a href='{resetLink}' style='padding: 10px 20px; background: #3b82f6; color: white; text-decoration: none; border-radius: 6px;'>Şifremi Sıfırla</a>
                </div>"
            };

            if (_env.IsDevelopment())
            {
                _logger.LogInformation("Geliştirme Modu - Şifre Sıfırlama Linki: {ResetLink}", resetLink);
            }

            if (string.IsNullOrWhiteSpace(resendApiKey) || resendApiKey.StartsWith("re_YOUR_KEY"))
            {
                _logger.LogWarning("Resend API Key yapılandırılmamış veya varsayılan değerde. E-posta gönderilmedi.");
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                request.Headers.Add("Authorization", $"Bearer {resendApiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(emailData), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Resend API e-posta gönderimi başarısız: {StatusCode} {Error}", response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gönderilirken beklenmeyen bir hata oluştu: {Message}", ex.Message);
            }
        }
    }
}
