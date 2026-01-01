namespace KeystoneCommerce.Application.DTOs.Order
{
    public class CouponDetailsDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public int DiscountPercentage { get; set; }
    }
}
