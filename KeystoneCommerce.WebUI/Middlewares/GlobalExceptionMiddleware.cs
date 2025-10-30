using Microsoft.AspNetCore.Diagnostics;
namespace KeystoneCommerce.WebUI.Middlewares;
public class GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Redirect("/Home/Error");
        return ValueTask.FromResult(true);
    }
}