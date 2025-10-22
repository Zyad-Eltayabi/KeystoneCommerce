const DeletedImages = [];
function removeGallary(element, imageName) {
    element.closest(".col").remove();
    DeletedImages.push(imageName);
}

document.getElementById("form").addEventListener("submit", function () {
    document.getElementById("DeletedImagesJson").value =
        JSON.stringify(DeletedImages);
});
