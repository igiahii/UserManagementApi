using System.Diagnostics;

namespace UserManagementAPI.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Log request
            var request = context.Request;
            _logger.LogInformation("Incoming Request: {Method} {Path}", request.Method, request.Path);

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context); // Proceed to next middleware

            stopwatch.Stop();

            // Log response
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation("Response Status: {StatusCode} | Duration: {Duration}ms",
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds);

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}