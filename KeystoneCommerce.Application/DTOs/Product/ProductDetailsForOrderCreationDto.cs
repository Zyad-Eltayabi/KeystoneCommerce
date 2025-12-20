namespace KeystoneCommerce.Application.DTOs.Product;

public class ProductDetailsForOrderCreationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? Discount { get; set; }
}