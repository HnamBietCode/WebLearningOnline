document.addEventListener("DOMContentLoaded", function () {
    // Preview image before upload
    const imageInput = document.getElementById('courseImage');
    imageInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            const previewContainer = document.querySelector('.preview-container');
            const uploadPrompt = document.querySelector('.upload-prompt');
            const previewImage = document.getElementById('imagePreview');

            reader.onload = function (e) {
                previewImage.src = e.target.result;
                previewContainer.classList.remove('d-none');
                uploadPrompt.classList.add('d-none');
            };
            reader.readAsDataURL(file);
        }
    });

    // Enhance form validation
    const form = document.querySelector('.needs-validation');
    form.addEventListener('submit', function (event) {
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
        }
        form.classList.add('was-validated');
    }, false);
});