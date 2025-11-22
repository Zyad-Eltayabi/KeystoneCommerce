using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Review;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMappingService _mappingService;
        private readonly IProductRepository _productRepository;

        public ReviewService(IReviewRepository reviewRepository, IMappingService mappingService, IProductRepository productRepository)
        {
            _reviewRepository = reviewRepository;
            _mappingService = mappingService;
            _productRepository = productRepository;
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

        private async Task<Result<CreateReviewDto>> validAsync(CreateReviewDto CreateReviewDto)
        {
            if (string.IsNullOrWhiteSpace(CreateReviewDto.Comment))
                return Result<CreateReviewDto>.Failure("Comment can't be null or empty");

            if (CreateReviewDto.Comment.Length > 2500)
                return Result<CreateReviewDto>.Failure("Comment can't be longer than 2500 characters");

            if (!await _productRepository.ExistsAsync(p => p.Id == CreateReviewDto.ProductId))
                return Result<CreateReviewDto>.Failure("Product does not exist");

            return Result<CreateReviewDto>.Success();
        }

        public async Task<Result<CreateReviewDto>> CreateNewReview(CreateReviewDto createReviewDto)
        {
            var validationResult = await validAsync(createReviewDto);
            if (!validationResult.IsSuccess)
                return validationResult;
            Review review = MapCreateReviewDtoToReview(createReviewDto);
            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();
            return Result<CreateReviewDto>.Success(createReviewDto);
        }

        private static Review MapCreateReviewDtoToReview(CreateReviewDto CreateReviewDto)
        {
            return new Review
            {
                ProductId = CreateReviewDto.ProductId,
                UserId = CreateReviewDto.UserId,
                Comment = CreateReviewDto.Comment,
                UserFullName = CreateReviewDto.UserFullName
            };
        }
    }
}