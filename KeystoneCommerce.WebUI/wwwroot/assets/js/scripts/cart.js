const selectors = {
    miniCart: ".mini_cart",
    cartItemCount: ".shopping_cart .item_count",
    overlay: ".body_overlay",
};

function setItemCount(count) {
    document.querySelector(selectors.cartItemCount).textContent = count;
}

function showError(err, defaultMessage = "An unexpected error occurred") {
    const message = err?.response?.data ?? defaultMessage;
    Swal.fire({
        title: "Oops!",
        text: message,
        icon: "error",
        timer: 2500,
    });
}

async function getMiniCart() {
    try {
        const response = await axios.get("/Cart/SmallCart");

        const miniCartEl = document.querySelector(selectors.miniCart);
        miniCartEl.innerHTML = response.data;

        const itemsCount = miniCartEl.querySelectorAll(".cart_item").length;
        setItemCount(itemsCount);
    } catch (err) {
        showError(err);
    }
}

async function updateCart(productId, count) {
    try {
        const response = await axios.post(
            "/Cart/UpdateSmallCart",
            { productId, count },
            { headers: { "Content-Type": "application/json" } }
        );

        setItemCount(response.data);
        await getMiniCart();
        if (count) showSuccess("Cart updated successfully");
        else {
            showSuccess("Product removed successfully");
        }
    } catch (err) {
        showError(err, "Error product not found");
    }
}



function showSuccess(message) {
    const Toast = Swal.mixin({
        toast: true,
        position: "top-start",
        showConfirmButton: false,
        timer: 1500,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.onmouseenter = Swal.stopTimer;
            toast.onmouseleave = Swal.resumeTimer;
        },
    });
    Toast.fire({
        icon: "success",
        title: message,
    });
}

function closeMiniCart() {
    document.querySelector(selectors.miniCart).classList.remove("active");
    document.querySelector(selectors.overlay).classList.remove("active");
}

document.addEventListener("DOMContentLoaded", getMiniCart);
