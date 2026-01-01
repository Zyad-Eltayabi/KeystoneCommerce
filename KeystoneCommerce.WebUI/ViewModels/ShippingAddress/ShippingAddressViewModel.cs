namespace KeystoneCommerce.WebUI.ViewModels.ShippingAddress
{
    public class ShippingAddressViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PostalCode { get; set; }
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string? Phone { get; set; }
    }
}
