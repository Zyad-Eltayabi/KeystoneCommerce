using KeystoneCommerce.Application.DTOs.Coupon;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Shared.Constants;

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
    private readonly IMappingService _mappingService;
    private readonly ICacheService _cacheService;

    public OrderService(
        IOrderRepository orderRepository,
        ILogger<OrderService> logger,
        IApplicationValidator<CreateOrderDto> createOrderValidator,
        IIdentityService identityService,
        IProductService productService,
        IProductRepository productRepository,
        ICouponService couponService,
        IShippingAddressService shippingAddressService,
        IShippingMethodService shippingMethodService,
        IMappingService mappingService,
        ICacheService cacheService)
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
        _mappingService = mappingService;
        _cacheService = cacheService;
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

        // Invalidate caches
        InvalidateOrderCaches();

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

        // Invalidate caches
        InvalidateOrderCaches();
        InvalidateOrderDetailsCache(orderId);
        InvalidateOrderPaymentCache();

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

        // Invalidate caches
        InvalidateOrderCaches();
        InvalidateOrderDetailsCache(orderId);

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

        // Invalidate caches
        InvalidateOrderCaches();
        InvalidateOrderDetailsCache(orderId);

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

    public async Task<string> GetOrderNumberByPaymentId(int paymentId)
    {
        string cacheKey = $"Order:GetByPayment:{paymentId}";
        
        var cachedOrderNumber = _cacheService.Get<string>(cacheKey);
        if (cachedOrderNumber is not null)
        {
            _logger.LogInformation("Cache hit for order number with payment ID: {PaymentId}", paymentId);
            return cachedOrderNumber;
        }

        _logger.LogInformation("Cache miss for order number with payment ID: {PaymentId}", paymentId);
        var orderNumber = await _orderRepository.GetOrderNumberByPaymentId(paymentId);
        
        _cacheService.Set(cacheKey, orderNumber, TimeSpan.FromMinutes(30));
        
        return orderNumber;
    }

    public async Task<OrderPaginatedResult<OrderDto>> GetAllOrdersPaginatedAsync(OrderPaginationParameters parameters)
    {
        _logger.LogInformation("Fetching paginated orders. PageNumber: {PageNumber}, PageSize: {PageSize}, Status: {Status}", 
            parameters.PageNumber, parameters.PageSize, parameters.Status);

        if (string.IsNullOrEmpty(parameters.SortBy))
        {
            parameters.SortBy = "CreatedAt";
            parameters.SortOrder = Sorting.Descending;
        }

        string cacheKey = $"Order:GetAllPaginated:{parameters.PageNumber}:{parameters.PageSize}:{parameters.Status?.ToString() ?? "All"}:{parameters.SortBy}:{parameters.SortOrder}:{parameters.SearchBy ?? "None"}:{parameters.SearchValue ?? "None"}";
        
        var cachedResult = _cacheService.Get<OrderPaginatedResult<OrderDto>>(cacheKey);
        if (cachedResult is not null)
        {
            _logger.LogInformation("Cache hit for paginated orders");
            return cachedResult;
        }

        _logger.LogInformation("Cache miss for paginated orders");
        var orders = await _orderRepository.GetOrdersPagedAsync(parameters);
        var orderDtos = _mappingService.Map<List<OrderDto>>(orders);

        _logger.LogInformation("Retrieved {Count} orders successfully", orderDtos.Count);

        var result = new OrderPaginatedResult<OrderDto>
        {
            Items = orderDtos,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = parameters.TotalCount,
            SortBy = parameters.SortBy,
            SortOrder = parameters.SortOrder,
            SearchBy = parameters.SearchBy,
            SearchValue = parameters.SearchValue,
            Status = parameters.Status
        };

        _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(2));
        
        return result;
    }

    public async Task<Result<OrderDetailsDto>> GetOrderDetailsByIdAsync(int orderId)
    {
        _logger.LogInformation("Fetching order details for order ID: {OrderId}", orderId);

        string cacheKey = $"Order:GetDetails:{orderId}";
        
        var cachedOrderDetails = _cacheService.Get<OrderDetailsDto>(cacheKey);
        if (cachedOrderDetails is not null)
        {
            _logger.LogInformation("Cache hit for order details with ID: {OrderId}", orderId);
            return Result<OrderDetailsDto>.Success(cachedOrderDetails);
        }

        _logger.LogInformation("Cache miss for order details with ID: {OrderId}", orderId);
        var order = await _orderRepository.GetOrderDetailsByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
            return Result<OrderDetailsDto>.Failure("Order not found.");
        }

        var userInfo = await _identityService.GetUserBasicInfoByIdAsync(order.UserId);
        if (userInfo is null)
        {
            _logger.LogWarning("User not found for order ID: {OrderId}, User ID: {UserId}", orderId, order.UserId);
            return Result<OrderDetailsDto>.Failure("User information not found.");
        }

        var orderDetailsDto = _mappingService.Map<OrderDetailsDto>(order);
        orderDetailsDto.User = userInfo;

        _cacheService.Set(cacheKey, orderDetailsDto, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Successfully retrieved order details for order ID: {OrderId}", orderId);
        return Result<OrderDetailsDto>.Success(orderDetailsDto);
    }

    public async Task<OrderDashboardDto> GetOrderDashboardDataAsync(OrderPaginationParameters parameters)
    {
        _logger.LogInformation("Fetching order dashboard data. PageNumber: {PageNumber}, PageSize: {PageSize}, Status: {Status}", 
            parameters.PageNumber, parameters.PageSize, parameters.Status);

        string cacheKey = $"Order:Dashboard:{parameters.PageNumber}:{parameters.PageSize}:{parameters.Status?.ToString() ?? "All"}:{parameters.SortBy ?? "None"}:{parameters.SortOrder ?? "None"}:{parameters.SearchBy ?? "None"}:{parameters.SearchValue ?? "None"}";
        
        var cachedDashboard = _cacheService.Get<OrderDashboardDto>(cacheKey);
        if (cachedDashboard is not null)
        {
            _logger.LogInformation("Cache hit for order dashboard data");
            return cachedDashboard;
        }

        _logger.LogInformation("Cache miss for order dashboard data");
        var paginatedOrders = await GetAllOrdersPaginatedAsync(parameters);
        var monthlyAnalytics = await _orderRepository.GetMonthlyAnalyticsAsync();
        var todayAnalytics = await _orderRepository.GetTodayAnalyticsAsync();

        _logger.LogInformation("Successfully retrieved order dashboard data");

        var dashboardDto = new OrderDashboardDto
        {
            PaginatedOrders = paginatedOrders,
            MonthlyAnalytics = monthlyAnalytics,
            TodayAnalytics = todayAnalytics
        };

        _cacheService.Set(cacheKey, dashboardDto, TimeSpan.FromMinutes(3));

        return dashboardDto;
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

    private void InvalidateOrderCaches()
    {
        _logger.LogInformation("Invalidating all paginated order caches and dashboard caches");
        _cacheService.RemoveByPrefix("Order:GetAllPaginated:");
        _cacheService.RemoveByPrefix("Order:Dashboard:");
    }

    private void InvalidateOrderDetailsCache(int orderId)
    {
        string cacheKey = $"Order:GetDetails:{orderId}";
        _logger.LogInformation("Invalidating order details cache for order ID: {OrderId}", orderId);
        _cacheService.Remove(cacheKey);
    }

    private void InvalidateOrderPaymentCache()
    {
        _logger.LogInformation("Invalidating all order payment caches");
        _cacheService.RemoveByPrefix("Order:GetByPayment:");
    }

    #endregion
}
