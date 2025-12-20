using Ganss.Xss;
using Hangfire;
using KeystoneCommerce.Infrastructure;
using KeystoneCommerce.WebUI.Extensions;
using KeystoneCommerce.WebUI.Filters;
using KeystoneCommerce.WebUI.Middleware;
using KeystoneCommerce.WebUI.Middlewares;
using KeystoneCommerce.WebUI.Profiles;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Application Services
builder.Services.AddApplicationServices();

// Register Infrastructure Services
builder.Services.AddInfrastructure(builder.Configuration, builder.Host);

// Register AutoMapper
builder.Services.AddAutoMapper(a => { a.AddProfile<WebMappings>(); });

builder.Services.AddSingleton<HtmlSanitizer>();
builder.Services.AddExceptionHandler<GlobalExceptionMiddleware>();
builder.Services.AddScoped<RequestLoggingMiddleware>();
builder.Services.AddProblemDetails();
WebApplication app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapStaticAssets();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();