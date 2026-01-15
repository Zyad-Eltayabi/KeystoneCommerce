using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Filters;
using KeystoneCommerce.WebUI.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IMappingService _mappingService;

        public AccountController(IAccountService accountService, IMappingService mappingService)
        {
            _accountService = accountService;
            _mappingService = mappingService;
        }

        [HttpGet]
        [RedirectAuthenticatedUser]
        public IActionResult Register()
        {
            return View("Register");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RedirectAuthenticatedUser]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                RegisterDto registerDto = _mappingService.Map<RegisterDto>(model);
                Result<RegisterDto> result = await _accountService.RegisterAsync(registerDto);
                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Registration successful";
                    return RedirectToAction("Index", "Home");
                }
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
            }
            return View("Register", model);
        }

        [HttpGet]
        [RedirectAuthenticatedUser]
        public IActionResult Login(string returnUrl = "")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RedirectAuthenticatedUser]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                LoginDto loginDto = _mappingService.Map<LoginDto>(model);
                Result<RegisterDto> result = await _accountService.LoginAsync(loginDto);
                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Login successful!, Welcome Back.";
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        return Redirect(model.ReturnUrl);
                    return RedirectToAction("Index", "Home");
                }

                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
            }
            return View("Login", model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View("AccessDenied");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            bool isLoggedOut = await _accountService.LogoutAsync();
            if (isLoggedOut)
            {
                TempData["SuccessMessage"] = "You have been logged out successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Logout failed. Please try again.";
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [RedirectAuthenticatedUser]
        public IActionResult ForgotPassword()
        {
            return View("ForgotPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RedirectAuthenticatedUser]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
                await _accountService.SendPasswordResetLinkAsync(model.Email);

            TempData["SuccessMessage"] = "If an account with that email exists, " +
                "a recovery email has been sent.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return BadRequest("Invalid password reset request.");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ResetPasswordDto dto = _mappingService.Map<ResetPasswordDto>(model);
                var result = await _accountService.ResetPasswordAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Password has been reset successfully. You can now log in with your new password.";
                    return RedirectToAction("Login");
                }
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
            }
            return View(model);
        }
    }
}
