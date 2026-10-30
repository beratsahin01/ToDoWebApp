using System.Net;
using System.Text.Json;

namespace ToDoWebApp.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İşlem sırasında beklenmeyen bir hata oluştu: {Message} [Path: {Path}]", ex.Message, httpContext.Request.Path);
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var isDevelopment = _env.IsDevelopment();
                var response = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "Sistemsel bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.",
                    Detailed = isDevelopment ? exception.Message : null
                };

                var jsonResponse = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(jsonResponse);
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
