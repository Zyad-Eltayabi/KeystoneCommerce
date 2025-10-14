using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations
{
    public class ProductGalleryConfiguration : IEntityTypeConfiguration<ProductGallery>
    {
        public void Configure(EntityTypeBuilder<ProductGallery> builder)
        {
            builder.ToTable("ProductGalleries");
            builder.HasKey(pg => pg.Id);
            builder.Property(pg => pg.ImageName)
                .IsRequired()
                .HasMaxLength(50);
            builder.Property(pg => pg.ProductId)
                .IsRequired();
        }
    }
}
