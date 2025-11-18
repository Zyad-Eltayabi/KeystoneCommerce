using Ganss.Xss;
using Serilog.Context;

namespace KeystoneCommerce.WebUI.Middlewares;

public class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly HtmlSanitizer _sanitizer;

    public RequestLoggingMiddleware(
        ILogger<RequestLoggingMiddleware> logger,
        HtmlSanitizer sanitizer)
    {
        _logger = logger;
        _sanitizer = sanitizer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string traceId = Guid.NewGuid().ToString();
        DateTime start = DateTime.UtcNow;
        HttpRequest request = context.Request;
        string user = context.User?.Identity?.IsAuthenticated == true
            ? "Authenticated User" : "Anonymous";
        string safeMethod = _sanitizer.Sanitize(request.Method);
        string safePath = _sanitizer.Sanitize(request.Path);

        using (LogContext.PushProperty("TraceId", traceId))
        {
            _logger.LogInformation("Incoming request {@RequestInfo}", new
            {
                Method = safeMethod,
                Path = safePath,
                User = user,
                Ip = context.Connection.RemoteIpAddress?.ToString()
            });
        }

        try
        {
            await next(context);
            using (LogContext.PushProperty("TraceId", traceId))
            {
                TimeSpan duration = DateTime.UtcNow - start;
                _logger.LogInformation(
                    "Completed {Method} {Path} with {StatusCode} in {Elapsed:0.000} ms",
                    safeMethod, safePath,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("TraceId", traceId))
            {
                TimeSpan duration = DateTime.UtcNow - start;

                _logger.LogError(ex,
                    "Request {Method} {Path} failed with error: {ErrorMessage} after {Elapsed:0.000} ms",
                    safeMethod, safePath,
                    ex.Message,
                    duration.TotalMilliseconds);
            }
            throw;
        }
    }
}
