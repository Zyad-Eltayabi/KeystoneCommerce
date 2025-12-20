using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KeystoneCommerce.Infrastructure.Persistence.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Banner> Banners { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<ProductGallery> ProductGalleries { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<ShippingMethod> ShippingMethods { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<ShippingAddress> ShippingAddresses { get; set; } = null!;
    public DbSet<Coupon> Coupons { get; set; } = null!;
    public DbSet<InventoryReservation> InventoryReservations { get; set; } = null!;
}
