using Microsoft.AspNetCore.Identity;

namespace KeystoneCommerce.Infrastructure.Persistence.Identity
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    }
}
