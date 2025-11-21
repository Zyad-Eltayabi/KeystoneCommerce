using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeystoneCommerce.WebUI.Controllers
{
    [Route("[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<ActionResult> GetProductReviews(PaginationParameters paginationParameters)
        {
            var reviews = await _reviewService.GetProductReviews(paginationParameters);
            if (reviews is null)
                return NotFound();
            return Ok(reviews);
        }
    }
}
