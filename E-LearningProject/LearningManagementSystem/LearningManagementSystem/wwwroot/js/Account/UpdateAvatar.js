// Function to confirm account deletion
function confirmDelete() {
    Swal.fire({
        title: 'Bạn có chắc chắn?',
        text: "Tài khoản của bạn sẽ bị đóng và không thể khôi phục!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#6200EA',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Xác nhận đóng',
        cancelButtonText: 'Hủy bỏ'
    }).then((result) => {
        if (result.isConfirmed) {
            document.getElementById('deleteAccountForm').submit();
        }
    });
}

// Update file name display when file is selected
document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('avatarFile');
    const fileInfo = document.querySelector('.file-info');

    if (fileInput && fileInfo) {
        fileInput.addEventListener('change', function () {
            if (this.files.length > 0) {
                fileInfo.textContent = this.files[0].name;
            } else {
                fileInfo.textContent = 'Chưa có tệp nào được chọn';
            }
        });
    }

    // Auto-hide notifications after 5 seconds
    const notifications = document.querySelectorAll('.notification');
    notifications.forEach(notification => {
        setTimeout(() => {
            notification.style.opacity = '0';
            setTimeout(() => {
                notification.style.display = 'none';
            }, 300);
        }, 5000);
    });
});