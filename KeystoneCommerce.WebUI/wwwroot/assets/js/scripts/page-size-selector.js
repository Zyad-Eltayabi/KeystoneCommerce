document.getElementById("pageSizeSelect").addEventListener("change", function () {
    const newSize = this.value;
    const url = new URL(window.location.href);
    url.searchParams.set("pageSize", newSize);
    url.searchParams.set("pageNumber", 1);
    window.location.href = url.toString();
});