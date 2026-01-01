namespace KeystoneCommerce.WebUI.ViewModels.OrderItem
{
    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public int ProductId { get; set; }
    }
}
