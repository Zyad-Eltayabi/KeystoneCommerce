using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.WebUI.ViewModels.Orders;

namespace KeystoneCommerce.WebUI.Controllers;

[Route("Admin/[controller]")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;

    public OrdersController(IOrderService orderService, IMapper mapper)
    {
        _orderService = orderService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] OrderPaginationParameters parameters)
    {
        var paginatedOrders = await _orderService.GetAllOrdersPaginatedAsync(parameters);
        return View(paginatedOrders);
    }

    [HttpGet]
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _orderService.GetOrderDetailsByIdAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Order not found.";
            return RedirectToAction("Index");
        }

        var orderDetailsViewModel = _mapper.Map<OrderDetailsViewModel>(result.Data);
        return View(orderDetailsViewModel);
    }
}
