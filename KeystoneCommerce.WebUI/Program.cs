using KeystoneCommerce.Infrastructure;
using KeystoneCommerce.WebUI.Extensions;
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

builder.Services.AddExceptionHandler<GlobalExceptionMiddleware>();
builder.Services.AddScoped<RequestLoggingMiddleware>();

WebApplication app = builder.Build();

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


app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestLoggingMiddleware>();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();