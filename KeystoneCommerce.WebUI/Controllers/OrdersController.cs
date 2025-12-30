using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{
    [Route("Admin/[controller]")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] OrderPaginationParameters parameters)
        {
            var paginatedOrders = await _orderService.GetAllOrdersPaginatedAsync(parameters);
            return View(paginatedOrders);
        }
    }
}
