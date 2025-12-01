using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
{
    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        builder.ToTable("ShippingAddresses");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(sa => sa.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(sa => sa.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sa => sa.PostalCode)
            .HasMaxLength(20);

        builder.Property(sa => sa.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sa => sa.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sa => sa.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sa => sa.Phone)
            .HasMaxLength(20);

        builder.Property(sa => sa.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(sa => sa.Order)
            .WithOne(o => o.ShippingAddress)
            .HasForeignKey<Order>(o => o.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sa => sa.UserId);

        builder.HasIndex(sa => sa.Email);
    }
}