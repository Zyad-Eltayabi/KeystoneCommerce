using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace KeystoneCommerce.WebUI.Middleware;
public class GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var isApi = httpContext.Request.Path.StartsWithSegments("/api");

        if (isApi)
        {
            httpContext.Response.StatusCode = 500;
            httpContext.Response.ContentType = "application/json";

            var problem = new ProblemDetails()
            {
                Title = "Server Error",
                Status = 500,
                Detail = "An unexpected error occurred."
            };

            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }
        else
        {
            httpContext.Response.Redirect("/Home/Error");
        }
        return true;
    }
}