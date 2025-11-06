using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Register()
        {
            return View("Register");
        }
    }
}
