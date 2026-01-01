using KeystoneCommerce.Application.DTOs.Review;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<PaginatedResult<ReviewDto>?> GetProductReviews(PaginationParameters parameters);
        Task<Result<CreateReviewDto>> CreateNewReview(CreateReviewDto createReviewDto);
    }
}