using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class ShippingAddressService : IShippingAddressService
    {
        private readonly IApplicationValidator<CreateShippingDetailsDto> _validator;
        private readonly IShippingAddressRepository _shippingAddressRepository;
        private readonly IMappingService _mappingService;
        private readonly ILogger<ShippingAddressService> _logger;

        public ShippingAddressService(
            IApplicationValidator<CreateShippingDetailsDto> validator,
            IShippingAddressRepository shippingAddressRepository,
            IMappingService mappingService,
            ILogger<ShippingAddressService> logger)
        {
            _validator = validator;
            _shippingAddressRepository = shippingAddressRepository;
            _mappingService = mappingService;
            _logger = logger;
        }

        public async Task<Result<int>> CreateNewAddress(CreateShippingDetailsDto createShippingDetailsDto)
        {
            _logger.LogInformation("Creating new shipping address for {Email}", createShippingDetailsDto.Email);

            var validationResult = _validator.Validate(createShippingDetailsDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Shipping address validation failed for {Email}. Errors: {ValidationErrors}",
                    createShippingDetailsDto.Email,
                    string.Join(", ", validationResult.Errors));
                return Result<int>.Failure(validationResult.Errors);
            }

            var shippingAddress = _mappingService.Map<ShippingAddress>(createShippingDetailsDto);
            shippingAddress.UserId = createShippingDetailsDto.UserId;
            await _shippingAddressRepository.AddAsync(shippingAddress);
            var result = await _shippingAddressRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError(
                    "Failed to save shipping address to database for {Email}",
                    createShippingDetailsDto.Email);
                return Result<int>.Failure("Failed to create shipping address.");
            }

            _logger.LogInformation(
                "Shipping address created successfully with ID {ShippingAddressId})",
                shippingAddress.Id);

            return Result<int>.Success(shippingAddress.Id);
        }
    }
}
