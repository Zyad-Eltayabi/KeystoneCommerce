using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(oi => oi.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(oi => oi.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        // Indexes
        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.ProductId);
    }
}