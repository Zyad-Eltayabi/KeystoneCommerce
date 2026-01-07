using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.WebUI.ViewModels.Payment;
using Microsoft.AspNetCore.Authorization;

namespace KeystoneCommerce.WebUI.Controllers;

[Route("Admin/[controller]")]
[Authorize(Roles = "Admin")]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IMapper _mapper;

    public PaymentsController(IPaymentService paymentService, IMapper mapper)
    {
        _paymentService = paymentService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PaymentPaginationParameters parameters)
    {
        var dashboardData = await _paymentService.GetPaymentDashboardDataAsync(parameters);
        var viewModel = _mapper.Map<PaymentDashboardViewModel>(dashboardData);
        return View(viewModel);
    }

    [HttpGet]
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _paymentService.GetPaymentDetailsByIdAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Payment not found.";
            return RedirectToAction("Index");
        }

        var paymentDetailsViewModel = _mapper.Map<PaymentDetailsViewModel>(result.Data);
        return View(paymentDetailsViewModel);
    }
}
