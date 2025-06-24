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

// Add animation to notifications
document.addEventListener('DOMContentLoaded', function () {
    const notifications = document.querySelectorAll('.notification');
    notifications.forEach(notification => {
        // Auto-hide notifications after 5 seconds
        setTimeout(() => {
            notification.style.opacity = '0';
            setTimeout(() => {
                notification.style.display = 'none';
            }, 300);
        }, 5000);
    });
});