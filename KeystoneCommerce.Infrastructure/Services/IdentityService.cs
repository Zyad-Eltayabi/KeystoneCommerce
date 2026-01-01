using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using KeystoneCommerce.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;


        public IdentityService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        private async Task<List<string>> SaveUserAsync(ApplicationUser user, string password)
        {
            List<string> userCreationErrors = new();
            var userCreationResult = await _userManager.CreateAsync(user, password);
            if (!userCreationResult.Succeeded)
            {
                userCreationErrors.AddRange(userCreationResult.Errors.Select(e => e.Description));
                return userCreationErrors;
            }
            var roleAssignmentResult = await _userManager.AddToRoleAsync(user, SystemRoles.User);
            if (!roleAssignmentResult.Succeeded)
                userCreationErrors.AddRange(roleAssignmentResult.Errors.Select(e => e.Description));
            return userCreationErrors;
        }

        public async Task<List<string>> CreateUserAsync(string fullName, string email, string password)
        {
            ApplicationUser user = CreateUserInstance(fullName, email);
            await EnsureDefaultUserRoleAsync();
            var userCreationErrors = await SaveUserAsync(user, password);
            if (userCreationErrors.Any())
                return userCreationErrors;
            IEnumerable<Claim> additionalClaims = new List<Claim>
            {
                new Claim("FullName", fullName)
            };
            await _signInManager.SignInWithClaimsAsync(user, isPersistent: false,additionalClaims);
            return userCreationErrors;
        }

        private async Task EnsureDefaultUserRoleAsync()
        {
            if (!await _roleManager.RoleExistsAsync(SystemRoles.User))
            {
                await _roleManager.CreateAsync(new ApplicationRole
                {
                    Name = SystemRoles.User,
                    Description = "Default role for regular users"
                });
            }
        }

        private static ApplicationUser CreateUserInstance(string fullName, string email)
        {
            return new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName
            };
        }

        public async Task<bool> LoginUserAsync(string email, string password, bool rememberMe)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || await _userManager.CheckPasswordAsync(user, password) is false)
                return false;
            IEnumerable<Claim> additionalClaims = new List<Claim>
            {
                new Claim("FullName", user.FullName)
            };
            await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, additionalClaims);
            return true;
        }

        public async Task<bool> LogoutUserAsync()
        {
            await _signInManager.SignOutAsync();
            return true;
        }

        public async Task<bool> IsUserExists(string email)
        {
            return await _userManager.FindByEmailAsync(email) is not null;
        }

        public async Task<bool> IsUserExistsById(string userId)
        {
            return await _userManager.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<List<string>> ResetPasswordAsync(string email, string token, string newPassword)
        {
            List<string> errors = new();
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                errors.Add("Invalid password reset request.");
                return errors;
            }

            // Decode the token that was passed in from the reset link
            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            var decodedToken = Encoding.UTF8.GetString(decodedBytes);

            // Attempt to reset the user's password with the new one
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
            // If successful, update the Security Stamp to invalidate any active sessions or tokens
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                return errors;
            }
            errors.AddRange(result.Errors.Select(e => e.Description));
            return errors;
        }

        public async Task<UserBasicInfoDto?> GetUserBasicInfoByIdAsync(string userId)
        {
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserBasicInfoDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty
                })
                .FirstOrDefaultAsync();

            return user;
        }
    }
}
