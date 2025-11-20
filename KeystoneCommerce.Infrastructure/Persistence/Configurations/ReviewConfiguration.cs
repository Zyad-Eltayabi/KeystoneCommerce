using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.CompilerServices;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Comment)
                .IsRequired()
                .HasMaxLength(2500);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // Relationship with Product
            builder.HasOne(x => x.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.Reviews)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for faster queries
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}