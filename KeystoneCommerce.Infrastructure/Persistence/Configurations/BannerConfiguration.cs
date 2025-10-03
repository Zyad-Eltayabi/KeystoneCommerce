using KeystoneCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeystoneCommerce.Infrastructure.Persistence.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("Banners");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(b => b.SubTitle)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(b => b.ImageName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(b => b.Link)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Priority)
            .IsRequired();
        
        builder.Property(b => b.BannerType)
            .HasConversion<int>()  
            .IsRequired();
    }
}