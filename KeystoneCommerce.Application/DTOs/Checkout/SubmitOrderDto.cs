using KeystoneCommerce.Application.DTOs.ShippingDetails;

namespace KeystoneCommerce.Application.DTOs.Checkout
{
    public class SubmitOrderDto
    {
        public string? CouponCode { get; set; }
        public string ShippingMethod { get; set; } = string.Empty;
        public int PaymentType { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Dictionary<int, int> ProductIdToQuantityMap { get; set; } = new();
        public CreateShippingDetailsDto ShippingDetails { get; set; } = new();
    }
}
