using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Application.DTOs.Order
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public OrderStatus Status { get; set; }
        public decimal SubTotal { get; set; } // sum of item line totals before shipping/discount.
        public decimal Total { get; set; }
        public decimal Shipping { get; set; }
        public decimal Discount { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}