using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class ShopController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}