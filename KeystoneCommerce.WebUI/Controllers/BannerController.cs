using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class BannerController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View("Index");
    }
}