using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Review;
using KeystoneCommerce.Domain.Entities;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<PaginatedResult<ReviewDto>?> GetProductReviews(PaginationParameters parameters);
        Task<Result<CreateReviewDto>> CreateNewReview(CreateReviewDto createReviewDto);
    }
}