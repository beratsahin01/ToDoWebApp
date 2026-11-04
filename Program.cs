using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Threading.RateLimiting;
using ToDoWebApp.Middlewares;
using ToDoWebApp.Models.Data;
using ToDoWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Güvenli Cookie Kimlik Doğrulama
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// 2. Response Compression (Brotli & Gzip ile veri transferini %70+ hızlandırma)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// 3. Rate Limiting (Kaba kuvvet ve istek taşması koruması)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-limit", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IEmailService, ResendEmailService>();

// 4. Güvenli Antiforgery (CSRF Koruması)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// 5. Veritabanı Bağlantısı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 6. Health Checks (Sağlık Denetimi)
builder.Services.AddHealthChecks();

// 7. Yüksek Performanslı Dağıtık Önbellekleme (Akıllı Fallback)
// Render veya yerel ortamda Redis bağlantısı yoksa, 5-10 saniye beklemek yerine
// 0ms gecikmeli bellek içi (In-Memory) dağıtık önbelleğe geçerek yanıt sürelerini mikrosaniyelere düşürür.
var redisConn = builder.Configuration.GetConnectionString("Redis");
bool isRemoteRedisConfigured = !string.IsNullOrWhiteSpace(redisConn)
    && !redisConn.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
    && !redisConn.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase);

if (isRemoteRedisConfigured)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "ToDoWebApp_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 8. Global Exception Handler
app.UseMiddleware<ExceptionMiddleware>();

// 9. Ortama Göre Hata & Swagger Yönetimi
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS (HTTP Strict Transport Security)
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Güvenlik Başlıkları (Security Headers)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.MapStaticAssets();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Otomatik Veritabanı Migration Uygulaması
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration işlemi sırasında bir hata oluştu.");
    }
}

app.Run();
