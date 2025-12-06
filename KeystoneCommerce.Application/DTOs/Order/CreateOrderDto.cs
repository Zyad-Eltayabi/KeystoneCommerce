using KeystoneCommerce.Application.DTOs.ShippingDetails;

namespace KeystoneCommerce.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        public string ShippingMethod { get; set; } = string.Empty;
        public string? Coupon { get; set; } = string.Empty;
        public string UserId { get; set; } = null!;
        public string PaymentProvider { get; set; } = string.Empty;
        public CreateShippingDetailsDto ShippingDetails { get; set; } = null!;
        public Dictionary<int, int> ProductsWithQuantity { get; set; } = new();
    }
}
