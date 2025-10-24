let currentProductId = null;
let currentProductRow = null;

document.querySelectorAll(".delete-btn").forEach((button) => {
    button.addEventListener("click", function () {
        currentProductId = this.getAttribute("data-product-id");
        currentProductRow = this.closest("tr");
    });
});

function hideModel(id) {
    const modal = bootstrap.Modal.getInstance(document.getElementById(`${id}`));
    modal.hide();
}

function showModel(id) {
    const modal = new bootstrap.Modal(document.getElementById(`${id}`));
    modal.show();
}
function confirmAction() {
    if (currentProductId != null) {
        let url = `/Admin/Products/Delete/${currentProductId}`;
        axios
            .delete(url, {
                headers: {
                    "Content-Type": "application/json",
                },
            })
            .then(function (response) {
                hideModel("confirmModal");
                currentProductRow.remove();
                showModel("successModal");
                setTimeout(function () {
                    hideModel("successModal");
                }, 2000);
            })
            .catch(function (error) {
                console.log(error);
                alert(error.response.data);
                hideModel("confirmModal");
            });
    }
}
