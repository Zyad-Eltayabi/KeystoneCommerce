const baseUrl = "https://localhost:7204";

const reviewsNav = document.querySelector(
    ".product_info_button ul li a[href='#reviews']"
);

const productId = reviewsNav.dataset.productId;
const reviewsBox = document.querySelector(".reviews-box");
const loadMoreBtn = document.querySelector(".load-more");
const reviewsCountEl = document.querySelector(".reviews-count");

const pagination = {
    pageNumber: 1,
    pageSize: 10,
    sortBy: "CreatedAt",
    sortOrder: "Desc",
    searchBy: "productId",
    searchValue: productId,
};

function buildQuery(params) {
    return new URLSearchParams(params).toString();
}

function buildUrl() {
    return `${baseUrl}/Reviews?${buildQuery(pagination)}`;
}

function updatePagination(result) {
    Object.assign(pagination, {
        pageNumber: result.pageNumber + 1,
        pageSize: result.pageSize,
        sortBy: result.sortBy,
        sortOrder: result.sortOrder,
        searchBy: result.searchBy,
        searchValue: result.searchValue,
    });

    reviewsCountEl.textContent = result.totalCount;
    loadMoreBtn.style.display = result.hasNext ? "block" : "none";
}

function renderReview(review) {
    return `
        <div class="review-item">
            <div class="reviews_comment_box">
                <div class="comment_thmb">
                    <img src="/assets/img/blog/comment2.jpg" alt="">
                </div>
                <div class="comment_text">
                    <div class="reviews_meta">
                        <div class="star_rating">
                            <ul class="d-flex">
                                ${"<li><i class='icon-star'></i></li>".repeat(
                                    5
                                )}
                            </ul>
                        </div>
                        <p><strong>${review.userFullName}</strong> - ${new Date(review.createdAt).toLocaleString("en-US")}</p>
                        <span>${review.comment}</span>
                    </div>
                </div>
            </div>
        </div>`;
}

function renderReviews(items) {
    reviewsBox.insertAdjacentHTML(
        "beforeend",
        items.map(renderReview).join("")
    );
}

async function fetchReviews() {
    try {
        const { data } = await axios.get(buildUrl());
        renderReviews(data.items);
        updatePagination(data);
    } catch (ex) {
        alert(ex.response?.data ?? "Unexpected error");
    }
}

reviewsNav.addEventListener("click", fetchReviews);
loadMoreBtn.addEventListener("click", fetchReviews);
