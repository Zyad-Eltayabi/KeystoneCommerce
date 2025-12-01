using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        {
        
            builder.HasKey(o => o.Id);
        
            builder.Property(o => o.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
        
            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.Status)
                .IsRequired();
                
            builder.Property(o => o.Total)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            
            builder.Property(o => o.SubTotal)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            
            builder.Property(o => o.Shipping)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            
            builder.Property(o => o.Discount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
        
            builder.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(o => o.IsPaid)
                .IsRequired();
                
        
            builder.Property(o => o.CreatedAt)
                .IsRequired();
        
            builder.Property(o => o.UpdatedAt);

            builder.Property(o => o.UserId)
                .IsRequired()
                .HasMaxLength(450);
        
            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}