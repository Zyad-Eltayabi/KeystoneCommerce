using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Checkout;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<Result<OrderDto>> CreateNewOrder(CreateOrderDto createOrderDto);
    }
}
