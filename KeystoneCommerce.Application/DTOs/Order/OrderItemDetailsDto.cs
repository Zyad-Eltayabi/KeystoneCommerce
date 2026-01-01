namespace KeystoneCommerce.Application.DTOs.Order
{
    public class OrderItemDetailsDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public int ProductId { get; set; }
    }
}
