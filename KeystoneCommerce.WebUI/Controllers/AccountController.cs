using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Filters;
using KeystoneCommerce.WebUI.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{
    [RedirectAuthenticatedUser]
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
        public IActionResult Register()
        {
            return View("Register");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public IActionResult Login()
        {
            return View("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                LoginDto loginDto = _mappingService.Map<LoginDto>(model);
                Result<RegisterDto> result = await _accountService.LoginAsync(loginDto);
                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Login successful!, Welcome Back.";
                    return RedirectToAction("Index", "Home");
                }
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
            }
            return View("Login", model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View("AccessDenied");
        }
    }
}
