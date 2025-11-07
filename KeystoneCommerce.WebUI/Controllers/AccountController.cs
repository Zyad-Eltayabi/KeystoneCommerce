using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.Interfaces.Services;
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
                var registerDto = _mappingService.Map<RegisterDto>(model);
                var result = await _accountService.RegisterAsync(registerDto);
                if (result.IsSuccess)
                    return RedirectToAction("Index", "Home");
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
                var loginDto = _mappingService.Map<LoginDto>(model);
                var result = await _accountService.LoginAsync(loginDto);
                if (result.IsSuccess)
                    return RedirectToAction("Index", "Home");
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
            }
            return View("Login", model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View("AccessDenied");
        }
    }
}
