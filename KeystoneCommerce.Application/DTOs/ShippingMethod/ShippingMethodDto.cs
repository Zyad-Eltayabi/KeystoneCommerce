namespace KeystoneCommerce.Application.DTOs.ShippingMethod
{
    public class ShippingMethodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string EstimatedDays { get; set; } = string.Empty;
    }
}