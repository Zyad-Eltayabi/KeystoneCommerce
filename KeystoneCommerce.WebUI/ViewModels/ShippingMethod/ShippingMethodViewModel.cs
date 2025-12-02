namespace KeystoneCommerce.WebUI.ViewModels.ShippingMethod
{
    public class ShippingMethodViewModel
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string EstimatedDays { get; set; } = string.Empty;
    }
}
