using KeystoneCommerce.Infrastructure;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using KeystoneCommerce.WebUI.Extensions;
using KeystoneCommerce.WebUI.Profiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add database context using SQL Server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register Application Services
builder.Services.AddApplicationServices();

// Register Infrastructure Services
builder.Services.AddInfrastructure();

// Register AutoMapper
builder.Services.AddAutoMapper(a =>
{
    a.AddProfile<WebMappings>();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();