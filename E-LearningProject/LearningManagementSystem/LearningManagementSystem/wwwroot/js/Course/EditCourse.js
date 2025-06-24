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
        } // Đã thêm dấu đóng ngoặc nhọn cho if statement
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

    // Add focus effect to form controls
    document.querySelectorAll('.form-control, .form-select').forEach(element => {
        element.addEventListener('focus', function () {
            this.classList.add('shadow-sm');
        });
        element.addEventListener('blur', function () {
            this.classList.remove('shadow-sm');
        });
    });

    // --- Disable nút Lưu thay đổi nếu không có thay đổi ---
    const courseNameInput = document.getElementById('courseName');
    const descInput = document.querySelector('textarea[name="Description"]');
    const priceInput = document.querySelector('input[name="Price"]');
    const saveBtn = document.querySelector('button[type="submit"]');

    if (courseNameInput && descInput && priceInput && saveBtn) {
        const original = {
            name: courseNameInput.value,
            desc: descInput.value,
            price: priceInput.value
        };

        function isChanged() {
            const nameNow = courseNameInput.value.trim();
            const descNow = descInput.value.trim();
            const priceNow = priceInput.value.replace(',', '.').trim();
            const origName = original.name.trim();
            const origDesc = original.desc.trim();
            const origPrice = original.price.replace(',', '.').trim();
            const priceChanged = parseFloat(priceNow || 0) !== parseFloat(origPrice || 0);
            return nameNow !== origName || descNow !== origDesc || priceChanged;
        }

        function checkChanged() {
            saveBtn.disabled = !isChanged();
        }

        saveBtn.disabled = true;
        courseNameInput.addEventListener('input', checkChanged);
        descInput.addEventListener('input', checkChanged);
        priceInput.addEventListener('input', checkChanged);
    }
});