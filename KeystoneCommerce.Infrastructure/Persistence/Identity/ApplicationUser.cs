using KeystoneCommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace KeystoneCommerce.Infrastructure.Persistence.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
