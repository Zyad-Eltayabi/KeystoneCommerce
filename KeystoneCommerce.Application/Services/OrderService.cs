using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Coupon;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly IApplicationValidator<CreateOrderDto> _createOrderValidator;
    private readonly IIdentityService _identityService;
    private readonly IProductService _productService;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICouponService _couponService;
    private readonly IShippingAddressService _shippingAddressService;
    private readonly IShippingMethodService _shippingMethodService;

    public OrderService(
        IOrderRepository orderRepository,
        ILogger<OrderService> logger,
        IApplicationValidator<CreateOrderDto> createOrderValidator,
        IIdentityService identityService,
        IProductService productService,
        IProductRepository productRepository,
        ICouponService couponService,
        IShippingAddressService shippingAddressService,
        IShippingMethodService shippingMethodService)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _createOrderValidator = createOrderValidator;
        _identityService = identityService;
        _productService = productService;
        _productRepository = productRepository;
        _couponService = couponService;
        _shippingAddressService = shippingAddressService;
        _shippingMethodService = shippingMethodService;
    }

    public async Task<Result<OrderDto>> CreateNewOrder(CreateOrderDto order)
    {
        _logger.LogInformation("Creating new order for user: {UserId}", order.UserId);

        // Validate create order DTO
        var validationResult = _createOrderValidator.Validate(order);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for order creation. Errors: {Errors}",
                String.Join(", ", validationResult.Errors));
            return Result<OrderDto>.Failure(validationResult.Errors);
        }

        // validate user exists
        if (!await _identityService.IsUserExistsById(order.UserId))
        {
            _logger.LogWarning("Order creation failed - User not found: {UserId}", order.UserId);
            return Result<OrderDto>.Failure("Please sign in first to complete your order.");
        }

        // validate coupon if exists
        CouponDto couponResult = new();
        var couponValidationResult = await ValidateCouponCode(order.Coupon);
        if (!couponValidationResult.IsSuccess)
            return Result<OrderDto>.Failure(couponValidationResult.Errors);
        couponResult = couponValidationResult.Data!;


        // validate shipping method
        var shippingMethod = await _shippingMethodService.GetShippingMethodByNameAsync(order.ShippingMethod);
        if (shippingMethod is null)
        {
            _logger.LogWarning("Order creation failed - Invalid shipping method: {ShippingMethod}", order.ShippingMethod);
            return Result<OrderDto>.Failure("Invalid shipping method.");
        }

        // validate products
        var productIds = order.ProductsWithQuantity.Keys.ToList();
        if (!await _productService.AreAllProductsExistAsync(productIds))
        {
            _logger.LogWarning("Order creation failed - One or more products not found. Product IDs: {ProductIds}",
                String.Join(", ", productIds));
            return Result<OrderDto>.Failure("One or more products not found.");
        }

        // validate shipping address
        order.ShippingDetails.UserId = order.UserId;
        var createdShippingAddressResult = await _shippingAddressService.CreateNewAddress(order.ShippingDetails);
        if (!createdShippingAddressResult.IsSuccess)
        {
            _logger.LogWarning("Order creation failed - Invalid shipping address details.");
            return Result<OrderDto>.Failure("Invalid shipping address details.");
        }
        var createdShippingAddressId = createdShippingAddressResult.Data;

        // Try to reserve stock
        var stockReservationResult = await TryReserveStockAsync(order.ProductsWithQuantity);
        if (!stockReservationResult.IsSuccess)
        {
            return Result<OrderDto>.Failure(stockReservationResult.Errors);
        }

        Order createNewOrder = await CreateAndSaveOrderAsync(order, couponResult, shippingMethod, productIds, createdShippingAddressId);

        OrderDto orderDto = CreateOrderDto(createNewOrder);
        return Result<OrderDto>.Success(orderDto);
    }

    public async Task<Result<bool>> MarkOrderAsPaid(int orderId)
    {
        _logger.LogInformation("Updating payment status for order ID: {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
            return Result<bool>.Failure("Order not found.");
        }

        if (order.IsPaid)
        {
            _logger.LogWarning("Order ID: {OrderId} is already marked as paid.", orderId);
            return Result<bool>.Failure("Order is already paid.");
        }

        order.IsPaid = true;
        order.Status = Domain.Enums.OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        _orderRepository.Update(order);
        var result = await _orderRepository.SaveChangesAsync();

        if (result == 0)
        {
            _logger.LogError("Failed to update payment status for order ID: {OrderId}", orderId);
            return Result<bool>.Failure("Failed to update order payment status.");
        }

        _logger.LogInformation("Payment status updated successfully for order ID: {OrderId}", orderId);
        return Result<bool>.Success();
    }

    public async Task<Result<string>> UpdateOrderStatusToFailed(int orderId)
    {
        _logger.LogInformation("Updating order status to failed for order ID: {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
            return Result<string>.Failure("Order not found.");
        }

        if (order.IsPaid)
        {
            _logger.LogWarning("Cannot mark order as failed - Order ID: {OrderId} is already paid.", orderId);
            return Result<string>.Failure("Cannot mark a paid order as failed.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Cannot mark order as failed - Order ID: {OrderId} is already cancelled.", orderId);
            return Result<string>.Failure("Cannot mark a cancelled order as failed.");
        }

        order.Status = OrderStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;
        _orderRepository.Update(order);
        var result = await _orderRepository.SaveChangesAsync();

        if (result == 0)
        {
            _logger.LogError("Failed to update order status to failed for order ID: {OrderId}", orderId);
            return Result<string>.Failure("Failed to update order status.");
        }

        _logger.LogInformation("Order status updated to failed successfully for order ID: {OrderId}", orderId);
        return Result<string>.Success();
    }

    public async Task<Result<string>> UpdateOrderStatusToCancelled(int orderId)
    {
        _logger.LogInformation("Updating order status to cancelled for order ID: {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
            return Result<string>.Failure("Order not found.");
        }

        if (order.IsPaid)
        {
            _logger.LogWarning("Cannot cancel order - Order ID: {OrderId} is already paid.", orderId);
            return Result<string>.Failure("Cannot cancel a paid order.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Order ID: {OrderId} is already cancelled.", orderId);
            return Result<string>.Failure("Order is already cancelled.");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        _orderRepository.Update(order);
        var result = await _orderRepository.SaveChangesAsync();
        if (result == 0)
        {
            _logger.LogError("Failed to update order status to cancelled for order ID: {OrderId}", orderId);
            return Result<string>.Failure("Failed to update order status.");
        }

        _logger.LogInformation("Order status updated to cancelled successfully for order ID: {OrderId}", orderId);
        return Result<string>.Success();
    }

    public async Task<bool> ReleaseReservedStock(int orderId)
    {
        _logger.LogInformation("Releasing reserved stock for order ID: {OrderId}", orderId);
        try
        {
            await _orderRepository.ReleaseReservedStock(orderId);
            _logger.LogInformation("Reserved stock released successfully for order ID: {OrderId}", orderId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error releasing reserved stock for order ID: {OrderId} with message {message}", orderId, e.Message);
            return false;
        }
    }

    #region private methods

    private static OrderDto CreateOrderDto(Order createNewOrder)
    {
        return new OrderDto
        {
            Id = createNewOrder.Id,
            OrderNumber = createNewOrder.OrderNumber,
            SubTotal = createNewOrder.SubTotal,
            Shipping = createNewOrder.Shipping,
            Discount = createNewOrder.Discount,
            Total = createNewOrder.Total,
            IsPaid = createNewOrder.IsPaid,
            Status = createNewOrder.Status,
            CreatedAt = createNewOrder.CreatedAt,
        };
    }

    private async Task<Order> CreateAndSaveOrderAsync(CreateOrderDto order, CouponDto couponResult, DTOs.ShippingMethod.ShippingMethodDto shippingMethod, List<int> productIds, int createdShippingAddressId)
    {
        List<ProductDetailsForOrderCreationDto> products = await _productRepository.GetProductsForOrderCreationAsync(productIds);

        List<OrderItem> items = BuildOrderItemsFromProducts(order, products);

        Order createNewOrder = await CreateOrderEntity(order, couponResult, shippingMethod, createdShippingAddressId, products, items);

        await _orderRepository.AddAsync(createNewOrder);
        await _orderRepository.SaveChangesAsync();
        _logger.LogInformation("Order created successfully with ID: {OrderId}", createNewOrder.Id);
        return createNewOrder;
    }

    private static List<OrderItem> BuildOrderItemsFromProducts(CreateOrderDto order, List<ProductDetailsForOrderCreationDto> products)
    {
        List<OrderItem> items = [];
        foreach (var productQuantityPair in order.ProductsWithQuantity)
        {
            var product = products.Where(x => x.Id == productQuantityPair.Key).FirstOrDefault();
            if (product is not null)
            {
                OrderItem orderItem = new()
                {
                    ProductName = product.Title,
                    ProductId = productQuantityPair.Key,
                    UnitPrice = product.Price,
                    Quantity = productQuantityPair.Value,
                };
                items.Add(orderItem);
            }
        }

        return items;
    }

    private async Task<Order> CreateOrderEntity(CreateOrderDto order, CouponDto couponResult, DTOs.ShippingMethod.ShippingMethodDto shippingMethod, int createdShippingAddressId, List<ProductDetailsForOrderCreationDto> products, List<OrderItem> items)
    {
        decimal subTotal = CalculateOrderSubTotal(products, order.ProductsWithQuantity);
        decimal total = CalculateOrderTotal(subTotal, shippingMethod.Price, couponResult.DiscountPercentage);
        decimal discountAmount = CalculateDiscountAmount(subTotal, couponResult.DiscountPercentage);
        Order createNewOrder = new()
        {
            OrderNumber = await GenerateOrderNumber(),
            UserId = order.UserId,
            ShippingAddressId = createdShippingAddressId,
            ShippingMethodId = shippingMethod.Id,
            SubTotal = CalculateOrderSubTotal(products, order.ProductsWithQuantity),
            Shipping = shippingMethod.Price,
            Discount = CalculateDiscountAmount(subTotal, couponResult.DiscountPercentage),
            Total = CalculateOrderTotal(subTotal, shippingMethod.Price, couponResult.DiscountPercentage),
            IsPaid = false,
            Status = OrderStatus.Processing,
            OrderItems = items,
            CouponId = couponResult.Id > 0 ? couponResult.Id : null
        };
        return createNewOrder;
    }

    private async Task<Result<CouponDto>> ValidateCouponCode(string? couponCode)
    {
        if (String.IsNullOrWhiteSpace(couponCode))
            return Result<CouponDto>.Success(new());

        var couponResult = await _couponService.GetCouponByName(couponCode);
        if (!couponResult.IsSuccess)
        {
            _logger.LogWarning("Invalid coupon code provided: {CouponCode}", couponCode);
            return Result<CouponDto>.Failure("Invalid coupon code.");
        }
        return couponResult;
    }

    private async Task<Result<bool>> TryReserveStockAsync(Dictionary<int, int> ProductsWithQuantity)
    {
        try
        {
            foreach (var item in ProductsWithQuantity)
            {
                await _productRepository.DecreaseProductStock(item.Key, item.Value);
            }
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reserving stock for products with message: {Message}", e.Message);
            return Result<bool>.Failure("Insufficient stock for one or more products.");
        }
    }

    private decimal CalculateOrderSubTotal(List<ProductDetailsForOrderCreationDto> products, Dictionary<int, int> ProductsWithQuantity)
    {
        decimal subTotal = 0;
        foreach (var product in products)
        {
            decimal productPrice = product.Price;
            if (product.Discount.HasValue && product.Discount.Value > 0)
            {
                productPrice -= (decimal)product.Discount;
            }
            int quantity = ProductsWithQuantity[product.Id];
            subTotal += productPrice * quantity;
        }
        return Math.Round(subTotal, 2);
    }

    private decimal CalculateOrderTotal(decimal subTotal, decimal shippingPrice, int couponDiscountAmount)
    {
        decimal total = subTotal + shippingPrice;
        if (couponDiscountAmount > 0)
        {
            decimal discountAmount = (subTotal * couponDiscountAmount) / 100;
            total -= discountAmount;
        }
        return total;
    }

    private decimal CalculateDiscountAmount(decimal subTotal, int couponDiscountAmount)
    {
        return couponDiscountAmount <= 0 ? 0 : (subTotal * couponDiscountAmount) / 100;
    }

    private async Task<string> GenerateOrderNumber()
    {
        string orderNumber;
        do
        {
            var random = new Random();
            var randomChars = new char[6];
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            for (int i = 0; i < 6; i++)
            {
                randomChars[i] = chars[random.Next(chars.Length)];
            }

            orderNumber = $"Ord-{new string(randomChars)}";
        }
        while (await _orderRepository.ExistsAsync(o => o.OrderNumber == orderNumber));
        return orderNumber;
    }

    #endregion
}
