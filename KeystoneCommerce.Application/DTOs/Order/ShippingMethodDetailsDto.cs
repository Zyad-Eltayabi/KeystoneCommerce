namespace KeystoneCommerce.Application.DTOs.Order
{
    public class ShippingMethodDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string EstimatedDays { get; set; } = null!;
    }
}
