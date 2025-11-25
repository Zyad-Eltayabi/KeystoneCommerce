document.querySelectorAll(".thumbnail").forEach((thumbnail) => {
    thumbnail.addEventListener("click", function () {
        document.getElementById("mainImage").src = this.src;
        document
            .querySelectorAll(".thumbnail")
            .forEach((thumb) => thumb.classList.remove("active"));
        this.classList.add("active");
    });
});
document.addEventListener("DOMContentLoaded", function () {
    const emailInput = document.getElementById("m-email");
    const submitBtn = document.getElementById("m-submit");
    const successDiv = document.querySelector(".mailchimp-success");
    const errorDiv = document.querySelector(".mailchimp-error");

    submitBtn.addEventListener("click", function (e) {
        e.preventDefault();

        // Clear previous messages
        successDiv.style.display = "none";
        errorDiv.style.display = "none";
        successDiv.textContent = "";
        errorDiv.textContent = "";

        const email = emailInput.value.trim();

        // Email validation regex
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!email) {
            errorDiv.textContent = "Please enter an email address";
            errorDiv.style.display = "block";
            return;
        }

        if (!emailRegex.test(email)) {
            errorDiv.textContent = "Please enter a valid email address";
            errorDiv.style.display = "block";
            return;
        }

        // Show success message
        successDiv.textContent = "Thank you for subscribing!";
        successDiv.style.display = "block";

        // Optional: Clear input after successful subscription
        emailInput.value = "";

        // Optional: Reset form after delay
        setTimeout(() => {
            successDiv.style.display = "none";
        }, 5000);
    });
});

function updateCartItems(productId) {
    let quantity = document.querySelector(".pro-qty input").value;
    updateCart(productId, quantity);
}
