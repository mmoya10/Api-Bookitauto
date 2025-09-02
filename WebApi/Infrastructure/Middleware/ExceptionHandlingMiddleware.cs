using System.Net;
using System.Text.Json;
using WebApi.DTOs.Common;

namespace WebApi.Infrastructure.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger, _env);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex, ILogger logger, IHostEnvironment env)
        {
            var (status, code) = ex switch
            {
                KeyNotFoundException        => (HttpStatusCode.NotFound, "not_found"),
                InvalidOperationException   => (HttpStatusCode.BadRequest, "bad_request"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "unauthorized"),
                _                           => (HttpStatusCode.InternalServerError, "server_error")
            };

            var error = new ApiError
            {
                Code = code,
                Message = ex.Message,
                Details = env.IsDevelopment() ? ex.ToString() : null
            };

            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
