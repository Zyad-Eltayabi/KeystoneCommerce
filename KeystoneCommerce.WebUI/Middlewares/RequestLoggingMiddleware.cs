using Serilog.Context;

namespace KeystoneCommerce.WebUI.Middlewares;

public class RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string traceId = Guid.NewGuid().ToString();
        DateTime start = DateTime.UtcNow;
        HttpRequest request = context.Request;
        string? user = context.User.Identity?.IsAuthenticated == true
            ? context.User.Identity.Name
            : "Anonymous";


        using (LogContext.PushProperty("TraceId", traceId))
        {
            logger.LogInformation("Incoming request {@RequestInfo}", new
            {
                request.Method,
                request.Path,
                Query = request.QueryString.ToString(),
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
                logger.LogInformation(
                    "Completed {Method} {Path} with {StatusCode} in {Elapsed:0.000} ms",
                    request.Method, request.Path, context.Response.StatusCode,
                    duration.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("TraceId", traceId))
            {
                TimeSpan duration = DateTime.UtcNow - start;
                logger.LogError(ex,
                    "Request {Method} {Path} failed with error: {ErrorMessage} after {Elapsed:0.000} ms",
                    request.Method,
                    request.Path,
                    ex.Message,
                    duration.TotalMilliseconds);
            }
            throw;
        }
    }
}