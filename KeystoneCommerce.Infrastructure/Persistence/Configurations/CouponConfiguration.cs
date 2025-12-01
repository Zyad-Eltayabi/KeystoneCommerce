using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.DiscountPercentage)
            .IsRequired();
        
        builder.Property(c => c.StartAt)
            .IsRequired();
        
        builder.Property(c => c.EndAt)
            .IsRequired();
        
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.Ignore(c => c.IsActive);
        
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Coupon)
            .HasForeignKey(o => o.CouponId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(c => c.Code)
            .IsUnique();
        
        builder.HasIndex(c => new { c.StartAt, c.EndAt });
    }
}