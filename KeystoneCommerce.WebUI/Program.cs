using KeystoneCommerce.Infrastructure;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using KeystoneCommerce.WebUI.Extensions;
using KeystoneCommerce.WebUI.Middlewares;
using KeystoneCommerce.WebUI.Profiles;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add database context using SQL Server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Configure the HostBuilder to use Serilog as the logging provider.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", path =>
            path.StartsWith("/assets") ||
            path.StartsWith("/css") ||
            path.StartsWith("/js") ||
            path.StartsWith("/img") ||
            path.StartsWith("/fonts") ||
            path.Contains(".woff") ||
            path.Contains(".ico")));
});

// Register Application Services
builder.Services.AddApplicationServices();

// Register Infrastructure Services
builder.Services.AddInfrastructure();

// Register AutoMapper
builder.Services.AddAutoMapper(a => { a.AddProfile<WebMappings>(); });

builder.Services.AddExceptionHandler<GlobalExceptionMiddleware>();
builder.Services.AddScoped<RequestLoggingMiddleware>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();