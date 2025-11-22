const baseUrl = "https://localhost:7204/api";

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
                        <p><strong>${review.userFullName}</strong> - ${new Date(
        review.createdAt
    ).toLocaleString("en-US")}</p>
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
        showError(ex.response?.data ?? "Unexpected error");
    }
}

reviewsNav.addEventListener("click", fetchReviews);
loadMoreBtn.addEventListener("click", fetchReviews);

// Submit new review
const submitReviewBtn = document.querySelector(
    ".product_review_form form button[type='submit']"
);
const reviewTextarea = document.querySelector(
    ".product_review_form form textarea"
);

function getReviewData() {
    return {
        ProductId: productId,
        Comment: reviewTextarea.value.trim(),
    };
}

function showSuccess(message) {
    Swal.fire({
        title: message,
        icon: "success",
        draggable: true,
    });
}

function showError(message) {
    Swal.fire({
        title: "Oops...",
        icon: "error",
        text: message,
    });
}

async function submitReview(data) {
    try {
        const response = await axios.post(`${baseUrl}/Reviews`, data);
        return { success: true, data: response.data };
    } catch (ex) {
        const detail = ex.response?.data?.Detail || "Unexpected error";
        return { success: false, data: detail };
    }
}

function addNewReview(data) {
    const review = {
        userFullName: data.userFullName,
        comment: data.comment,
        createdAt: new Date().toLocaleString("en-US"),
    };
    insertReviewInDOM(review);
    updateReviewsCount();
}

function insertReviewInDOM(review) {
    const html = renderReview(review);
    const h2 = reviewsBox.querySelector("h2");
    if (h2) {
        h2.insertAdjacentHTML("afterend", html);
    } else {
        reviewsBox.insertAdjacentHTML("afterbegin", html);
    }
}
function updateReviewsCount() {
    let span = reviewsBox.querySelector("h2 span");
    if (span) {
        span.textContent = parseInt(span.textContent) + 1;
    }
}

async function handleSubmitReview(e) {
    e.preventDefault();

    const reviewData = getReviewData();

    // Validate before sending
    if (!reviewData.Comment) {
        showError("Review cannot be empty.");
        return;
    }

    const result = await submitReview(reviewData);

    if (result.success) {
        addNewReview(result.data);
        showSuccess("Review submitted successfully.");
        reviewTextarea.value = "";
    } else {
        showError(result.data);
    }
}

submitReviewBtn.addEventListener("click", handleSubmitReview);
