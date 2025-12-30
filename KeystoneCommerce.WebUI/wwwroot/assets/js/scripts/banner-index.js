
let currentBannerId = null;
let currentBannerRow = null;

document.querySelectorAll('.delete-btn').forEach(button => {
    button.addEventListener('click', function () {
        currentBannerId = this.getAttribute('data-banner-id');
        currentBannerRow = this.closest('tr');
    });
});

function hideModel(id)
{
    const modal = bootstrap.Modal.getInstance(document.getElementById(`${id}`));
    modal.hide();
}

function showModel(id)
{
    const modal =new bootstrap.Modal(document.getElementById(`${id}`));
    modal.show();
}
function confirmAction() {
    if (currentBannerId != null) {
        let url = `/Admin/Banners/Delete/${currentBannerId}`;
        axios.delete(url, {
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(function (response) {
                hideModel("confirmModal");
                
                currentBannerRow.remove();

               showModel("successModal");


                setTimeout(function () {
                    hideModel("successModal");
                }, 3000)
            })
            .catch(function (error) {
                alert(error.response.data);
                hideModel("confirmModal");
            });
    }
}
