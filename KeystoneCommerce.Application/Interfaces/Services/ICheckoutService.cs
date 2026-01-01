using KeystoneCommerce.Application.DTOs.Checkout;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface ICheckoutService
    {
        Task<Result<OrderDto>> SubmitOrder(CreateOrderDto order);
    }
}
