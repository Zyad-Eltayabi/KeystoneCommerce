using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.Review;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMappingService _mappingService;

        public ReviewService(IReviewRepository reviewRepository, IMappingService mappingService)
        {
            _reviewRepository = reviewRepository;
            _mappingService = mappingService;
        }

        public async Task<PaginatedResult<ReviewDto>?> GetProductReviews(PaginationParameters parameters)
        {
            var reviews = await _reviewRepository.GetPagedAsync(parameters);
            if (reviews is null)
                return null;
            return new PaginatedResult<ReviewDto>
            {
                Items = _mappingService.Map<List<ReviewDto>>(reviews),
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalCount = parameters.TotalCount,
                SortBy = parameters.SortBy,
                SortOrder = parameters.SortOrder,
                SearchBy = parameters.SearchBy,
                SearchValue = parameters.SearchValue
            };
        }
    }
}