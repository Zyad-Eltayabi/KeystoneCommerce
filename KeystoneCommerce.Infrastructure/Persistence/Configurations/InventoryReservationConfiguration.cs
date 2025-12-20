using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(ir => ir.OrderId)
            .IsRequired();

        builder.Property(ir => ir.ExpiresAt);

        builder.Property(ir => ir.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ir => ir.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne(ir => ir.Order)
            .WithOne(o => o.InventoryReservation)
            .HasForeignKey<InventoryReservation>(ir => ir.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ir => ir.OrderId)
            .IsUnique();

        builder.HasIndex(ir => ir.Status);

        builder.HasIndex(ir => ir.ExpiresAt);
    }
}
