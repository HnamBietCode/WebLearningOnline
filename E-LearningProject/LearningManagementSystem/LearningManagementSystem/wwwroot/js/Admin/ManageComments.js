document.addEventListener("DOMContentLoaded", function () {
    // Kích hoạt tooltip
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Pagination settings
    const rowsPerPage = 5; // Số bình luận mỗi trang
    let currentPage = 1;
    let rows = document.querySelectorAll('table tbody tr');
    let totalRows = rows.length;
    let totalPages = Math.ceil(totalRows / rowsPerPage);

    // Function to display rows for the current page
    function displayPage(page) {
        currentPage = page;
        let start = (page - 1) * rowsPerPage;
        let end = start + rowsPerPage;

        rows.forEach((row, index) => {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        updatePagination();
    }

    // Function to update pagination controls
    function updatePagination() {
        let pagination = document.getElementById('pagination');
        pagination.innerHTML = '';

        // Không hiển thị pagination nếu chỉ có 1 trang hoặc không có dữ liệu
        if (totalPages <= 1) {
            return;
        }

        // Previous button
        let prevClass = currentPage === 1 ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            let activeClass = i === currentPage ? 'active' : '';
            pagination.innerHTML += `
                <li class="page-item ${activeClass}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        // Next button
        let nextClass = currentPage === totalPages ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${nextClass}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;
    }

    // Handle pagination click
    document.getElementById('pagination').addEventListener('click', function (e) {
        e.preventDefault();
        if (e.target.tagName === 'A') {
            let page = parseInt(e.target.getAttribute('data-page'));
            if (page >= 1 && page <= totalPages) {
                displayPage(page);
            }
        }
    });

    // Initial display
    if (totalRows > 0) {
        displayPage(1);
    }

    // Tìm kiếm bình luận - mở rộng để tìm theo nhiều tiêu chí
    const searchInput = document.getElementById('searchInput');
    searchInput.addEventListener('keyup', function () {
        const filter = searchInput.value.toLowerCase();
        const originalRows = Array.from(document.querySelectorAll('table tbody tr'));

        rows = originalRows.filter(row => {
            const userName = row.querySelector('td:nth-child(1)').textContent.toLowerCase();
            const courseName = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            const content = row.querySelector('td:nth-child(3)').textContent.toLowerCase();

            // Tìm kiếm theo tên người dùng, khóa học hoặc nội dung
            return userName.includes(filter) ||
                courseName.includes(filter) ||
                content.includes(filter);
        });

        totalRows = rows.length;
        totalPages = Math.ceil(totalRows / rowsPerPage);

        // Ẩn tất cả rows
        originalRows.forEach(row => row.style.display = 'none');

        // Hiển thị rows phù hợp
        if (totalRows > 0) {
            displayPage(1);
        } else {
            // Hiển thị thông báo không tìm thấy
            updatePagination();
        }
    });

    // Cập nhật placeholder cho search input
    searchInput.placeholder = "Tìm kiếm theo tên người dùng, khóa học hoặc nội dung...";
});

let formToDelete = null;
document.querySelectorAll('.btn-delete-comment').forEach(function(btn) {
    btn.addEventListener('click', function() {
        formToDelete = document.getElementById(this.getAttribute('data-form-id'));
        var modal = new bootstrap.Modal(document.getElementById('deleteCommentModal'));
        modal.show();
    });
});
const confirmBtn = document.getElementById('confirmDeleteBtn');
if (confirmBtn) {
    confirmBtn.addEventListener('click', function() {
        if (formToDelete) {
            formToDelete.submit();
        }
    });
}