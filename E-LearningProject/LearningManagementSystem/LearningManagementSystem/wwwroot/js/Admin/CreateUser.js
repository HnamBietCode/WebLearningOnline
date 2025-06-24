document.getElementById('avatarFile').addEventListener('change', function (e) {
    const file = e.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (event) {
            const preview = document.getElementById('avatar-preview');
            preview.classList.remove('d-none');
            preview.querySelector('img').src = event.target.result;
        }
        reader.readAsDataURL(file);
    }
});