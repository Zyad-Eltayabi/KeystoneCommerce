using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{
    [Route("Admin/[controller]")]
    public class ProductsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
