using System.ComponentModel.DataAnnotations;

namespace KeystoneCommerce.Application.DTOs.Account
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
