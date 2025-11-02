document.querySelectorAll('.thumbnail').forEach(thumbnail => {
    thumbnail.addEventListener('click', function () {
        document.getElementById('mainImage').src = this.src;
        document.querySelectorAll('.thumbnail').forEach(thumb => thumb.classList.remove('active'));
        this.classList.add('active');
    });
});
