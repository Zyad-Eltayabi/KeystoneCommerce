namespace KeystoneCommerce.WebUI.ViewModels.ShippingMethod
{
    public class OrderShippingMethodViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string EstimatedDays { get; set; } = null!;
    }
}
