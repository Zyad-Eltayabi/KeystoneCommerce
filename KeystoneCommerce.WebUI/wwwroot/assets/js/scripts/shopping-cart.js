document.addEventListener("DOMContentLoaded", () => {
    document
        .querySelectorAll(".cart_product_quantity input")
        .forEach((input) => {
            input.addEventListener("change", (e) => {
                const id = e.target.dataset.productId;
                const value = e.target.value;
                try {
                    updateCart(id, value);
                    if (value > 0) {
                        updateProductPrice(id);
                    } else {
                        removeProductFromPage(id);
                    }
                    updateTotalPrice();
                } catch (err) {
                    showError(err.data);
                }
            });
        });
});

function removeProductFromPage(productId) {
    let product = document.getElementById(productId);
    if (product) product.remove();
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

function updateProductPrice(productId) {
    let product = document.getElementById(productId);
    if (!product) return;
    let price = product
        .querySelector(".cart_product_price span")
        .textContent.substring(1); // remove $ symbol
    let quantity = product.querySelector(".cart_product_quantity input").value;
    let total = price * quantity;
    product.querySelector(
        ".cart_product_total span"
    ).textContent = `$${total.toFixed(2)}`;
}

function updateTotalPrice() {
    let totalPrice = 0;
    let productTotalElements = document.querySelectorAll(
        ".cart_product_total span"
    );
    let grandTotalElement = document.querySelector(".cart_grandtotal span");

    if (productTotalElements.length === 0) {
        grandTotalElement.textContent = 0;
        return;
    }

    productTotalElements.forEach((element) => {
        totalPrice += parseFloat(element.textContent.substring(1));
    });
    grandTotalElement.textContent = `$${totalPrice.toFixed(2)}`;
}

async function removeProduct(productId, quantity) {
    await updateCart(productId, quantity);
    removeProductFromPage(productId);
}