namespace KeystoneCommerce.Application.DTOs.ShippingDetails
{
    public class CreateShippingDetailsDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? PostalCode { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

    }
}
