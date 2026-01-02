using AutoMapper;
using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;

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
        var dashboardData = await _orderService.GetOrderDashboardDataAsync(parameters);
        var viewModel = _mapper.Map<OrderDashboardViewModel>(dashboardData);
        return View(viewModel);
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
