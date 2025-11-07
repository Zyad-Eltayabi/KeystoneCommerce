using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private const string defaultRole = "User";


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
            var roleAssignmentResult = await _userManager.AddToRoleAsync(user, defaultRole);
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
            await _signInManager.SignInAsync(user, isPersistent: false);
            return userCreationErrors;
        }

        private async Task EnsureDefaultUserRoleAsync()
        {
            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                await _roleManager.CreateAsync(new ApplicationRole
                {
                    Name = defaultRole,
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
    }
}
