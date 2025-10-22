const fileInputs = document.getElementsByClassName('fileUpload');
Array.from(fileInputs).forEach(fileInput => fileInput.addEventListener('change', function () {
    const label = this.nextElementSibling.querySelector('.upload-text');
    if (fileInput.files.length === 0) {
        label.textContent = 'Click to choose files';
    }
    else if (fileInput.files.length === 1) {
        label.textContent = fileInput.files[0].name;
    }
    else {
        label.textContent = `${fileInput.files.length} files selected`;
    }
}));