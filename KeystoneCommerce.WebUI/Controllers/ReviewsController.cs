using Ganss.Xss;
using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Review;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Review;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KeystoneCommerce.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly HtmlSanitizer _htmlSanitizer;

        public ReviewsController(IReviewService reviewService, HtmlSanitizer htmlSanitizer)
        {
            _reviewService = reviewService;
            _htmlSanitizer = htmlSanitizer;
        }

        [HttpGet]
        public async Task<ActionResult> GetProductReviews([FromQuery] PaginationParameters paginationParameters)
        {
            var reviews = await _reviewService.GetProductReviews(paginationParameters);
            if (reviews is null)
                return NotFound();
            return Ok(reviews);
        }

        [HttpPost]
        public async Task<ActionResult> CreateNewReview([FromBody] CreateReviewRequest review)
        {
            if (HttpContext.User is null)
                return Unauthorized(CreateErrorDetails(
                       StatusCodes.Status401Unauthorized,
                       "Authentication Required",
                       "You must be signed in to perform this action."
                   ));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fullName = User.FindFirstValue("FullName");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fullName))
                return Unauthorized(CreateErrorDetails(StatusCodes.Status401Unauthorized, "Unauthorized", "User information is missing"));

            CreateReviewDto model = CreateReviewDtoFromRequest(review, userId, fullName);

            var serviceResult = await _reviewService.CreateNewReview(model);

            if (serviceResult.IsSuccess)
                return Ok(serviceResult.Data);

            return BadRequest(CreateErrorDetails(StatusCodes.Status400BadRequest, "Review Creation Failed", string.Join(", ", serviceResult.Errors)));
        }

        private CreateReviewDto CreateReviewDtoFromRequest(CreateReviewRequest review, string userId, string fullName)
        {
            return new CreateReviewDto
            {
                ProductId = review.ProductId,
                Comment = _htmlSanitizer.Sanitize(review.Comment),
                UserId = userId,
                UserFullName = fullName
            };
        }
        private ProblemDetails CreateErrorDetails(int status, string title, string detail)
        {
            return new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            };
        }
    }
}
