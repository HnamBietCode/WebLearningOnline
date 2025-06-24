function toggleUserSelection(sendToAll) {
    const userSelection = document.getElementById('userSelection');
    userSelection.style.display = sendToAll ? 'none' : 'block';
}

// Hiệu ứng làm nổi bật hàng mới khi thêm thông báo
document.addEventListener('DOMContentLoaded', function () {
    const successAlert = document.querySelector('.alert-success');
    if (successAlert) {
        const firstRow = document.querySelector('tbody tr:first-child');
        if (firstRow) {
            firstRow.classList.add('table-info');
            setTimeout(() => {
                firstRow.classList.remove('table-info');
            }, 3000);
        }
    }
});