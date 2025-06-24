// Avatar preview
if (document.getElementById('avatarFile')) {
    document.getElementById('avatarFile').addEventListener('change', function (e) {
        if (e.target.files && e.target.files[0]) {
            const reader = new FileReader();
            reader.onload = function (event) {
                const imgPreview = document.querySelector('.avatar-preview img');
                if (imgPreview) {
                    imgPreview.src = event.target.result;
                } else {
                    const placeholder = document.querySelector('.avatar-placeholder');
                    if (placeholder) {
                        placeholder.parentNode.innerHTML = `<img src="${event.target.result}" alt="Avatar" class="img-thumbnail rounded-circle" style="width: 150px; height: 150px; object-fit: cover;" />`;
                    }
                }
            }
            reader.readAsDataURL(e.target.files[0]);
        }
        checkChanged();
    });
}

// Enable/disable nút Lưu thay đổi
const form = document.querySelector('form');
const btnSave = document.querySelector('button[type="submit"]');
const fullNameInput = document.getElementById('fullname');
const emailInput = document.getElementById('email');
const roleInput = document.getElementById('role');
const bioInput = document.getElementById('bio');
const passwordInput = document.getElementById('password');
let initial = {
    fullName: fullNameInput ? fullNameInput.value : '',
    email: emailInput ? emailInput.value : '',
    role: roleInput ? roleInput.value : '',
    bio: bioInput ? bioInput.value : '',
    avatar: '',
};
if (document.querySelector('.avatar-preview img')) {
    initial.avatar = document.querySelector('.avatar-preview img').src;
}

function isChanged() {
    if (fullNameInput && fullNameInput.value.trim() !== initial.fullName.trim()) return true;
    if (emailInput && emailInput.value.trim() !== initial.email.trim()) return true;
    if (roleInput && roleInput.value !== initial.role) return true;
    if (bioInput && bioInput.value.trim() !== initial.bio.trim()) return true;
    if (passwordInput && passwordInput.value.trim() !== '') return true;
    // Avatar
    if (document.querySelector('.avatar-preview img')) {
        if (document.querySelector('.avatar-preview img').src !== initial.avatar) return true;
    }
    if (document.getElementById('avatarFile') && document.getElementById('avatarFile').value) return true;
    return false;
}

function checkChanged() {
    if (btnSave) btnSave.disabled = !isChanged();
}

if (btnSave) {
    btnSave.disabled = true;
    [fullNameInput, emailInput, roleInput, bioInput, passwordInput].forEach(function (el) {
        if (el) el.addEventListener('input', checkChanged);
    });
    if (document.getElementById('avatarFile')) {
        document.getElementById('avatarFile').addEventListener('change', checkChanged);
    }
    form.addEventListener('input', checkChanged);
    form.addEventListener('change', checkChanged);
    form.addEventListener('submit', function () {
        btnSave.disabled = true;
        btnSave.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu...';
    });
}