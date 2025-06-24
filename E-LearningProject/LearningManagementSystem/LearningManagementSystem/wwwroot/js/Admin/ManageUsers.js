document.addEventListener("DOMContentLoaded", function () {
    // Kích hoạt tooltip Bootstrap
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Tìm kiếm người dùng
    const searchInput = document.getElementById('searchInput');
    const userCountElement = document.getElementById('userCount');
    const totalUsers = userCountElement ? parseInt(userCountElement.getAttribute('data-total')) : 0;

    searchInput.addEventListener('keyup', function () {
        const filter = searchInput.value.toLowerCase();
        const rows = document.querySelectorAll('table tbody tr');
        let visibleCount = 0;

        rows.forEach(row => {
            const userName = row.querySelector('td:nth-child(1)').textContent.toLowerCase();
            if (userName.includes(filter)) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        // Cập nhật số lượng hiển thị
        if (userCountElement) {
            userCountElement.textContent = visibleCount + ' / ' + totalUsers + ' người dùng';
        }
    });

    // Hiệu ứng khi hover vào nút
    const actionButtons = document.querySelectorAll('.btn-group .btn');
    actionButtons.forEach(button => {
        button.addEventListener('mouseenter', function () {
            this.classList.add('shadow-sm');
        });
        button.addEventListener('mouseleave', function () {
            this.classList.remove('shadow-sm');
        });
    });
});

let userFormToDelete = null;
document.querySelectorAll('.btn-delete-user').forEach(function(btn) {
    btn.addEventListener('click', function() {
        userFormToDelete = document.getElementById(this.getAttribute('data-form-id'));
        var modal = new bootstrap.Modal(document.getElementById('deleteUserModal'));
        modal.show();
    });
});
document.getElementById('confirmDeleteUserBtn').addEventListener('click', function() {
    if (userFormToDelete) {
        userFormToDelete.submit();
    }
});