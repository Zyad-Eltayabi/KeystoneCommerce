namespace KeystoneCommerce.Application.DTOs.Shop;

public class ProductCardDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? Discount { get; set; }
    public string? ImageName { get; set; }
}