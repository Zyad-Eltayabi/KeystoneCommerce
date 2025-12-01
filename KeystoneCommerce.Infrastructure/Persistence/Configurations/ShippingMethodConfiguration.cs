using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("ShippingMethods");

        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sm => sm.Description)
            .HasMaxLength(500);

        builder.Property(sm => sm.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(sm => sm.EstimatedDays)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasMany(sm => sm.Orders)
            .WithOne(o => o.ShippingMethod)
            .HasForeignKey(o => o.ShippingMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sm => sm.Name);
    }
}