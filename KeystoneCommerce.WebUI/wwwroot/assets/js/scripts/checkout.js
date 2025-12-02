let currentSubtotal = 0;
let currentDiscountPercentage = 0;
let currentShippingPrice = 0;

function initializeCheckout(subtotal, discountPercentage) {
    currentSubtotal = subtotal;
    currentDiscountPercentage = discountPercentage;
    
    const firstShippingMethod = document.querySelector('.shipping-method-radio:checked');
    if (firstShippingMethod) {
        currentShippingPrice = parseFloat(firstShippingMethod.dataset.price);
    }
    
    attachShippingMethodListeners();
}

function attachShippingMethodListeners() {
    const shippingRadios = document.querySelectorAll('.shipping-method-radio');
    
    shippingRadios.forEach(radio => {
        radio.addEventListener('change', function() {
            updateShippingPrice(this);
        });
    });
}

function updateShippingPrice(selectedRadio) {
    const newShippingPrice = parseFloat(selectedRadio.dataset.price);
    
    currentShippingPrice = newShippingPrice;
    
    const shippingPriceElement = document.getElementById('shippingPrice');
    if (shippingPriceElement) {
        shippingPriceElement.textContent = newShippingPrice.toFixed(2);
    }
    
    updateOrderTotal();
}

function updateOrderTotal() {
    const discount = currentSubtotal * (currentDiscountPercentage / 100);
    const total = currentSubtotal - discount + currentShippingPrice;
    
    const orderTotalElement = document.getElementById('orderTotal');
    if (orderTotalElement) {
        orderTotalElement.textContent = total.toFixed(2);
    }
}
