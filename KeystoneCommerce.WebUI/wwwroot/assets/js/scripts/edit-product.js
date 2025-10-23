const DeletedImages = [];
function removeGallary(element, imageName) {
    element.closest(".gallery-image-container").remove();
    DeletedImages.push(imageName);
}

document.getElementById("form").addEventListener("submit", function () {
    document.getElementById("DeletedImagesJson").value =
        JSON.stringify(DeletedImages);
});
