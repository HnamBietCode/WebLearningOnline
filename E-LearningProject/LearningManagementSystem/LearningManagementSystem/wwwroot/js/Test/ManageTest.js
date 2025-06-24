document.addEventListener("DOMContentLoaded", function () {
    // Khởi tạo tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Hiệu ứng fade out cho thông báo
    setTimeout(function () {
        var alerts = document.querySelectorAll('.alert');
        alerts.forEach(function (alert) {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    // Pagination settings
    const rowsPerPage = 5; // Số bài kiểm tra mỗi trang
    let currentPage = 1;
    let rows = document.querySelectorAll('.test-row');
    let totalRows = rows.length;
    let totalPages = Math.ceil(totalRows / rowsPerPage);

    // Function to display rows for the current page
    function displayPage(page) {
        currentPage = page;
        let start = (page - 1) * rowsPerPage;
        let end = Math.min(start + rowsPerPage, totalRows);

        rows.forEach((row, index) => {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        updatePagination();
        updatePaginationInfo();
    }

    // Function to update pagination info
    function updatePaginationInfo() {
        let start = (currentPage - 1) * rowsPerPage + 1;
        let end = Math.min(currentPage * rowsPerPage, totalRows);
        let infoText = totalRows > 0 ? `Hiển thị ${start}-${end} trên ${totalRows} bài kiểm tra` : '';
        const infoElem = document.getElementById('paginationInfo');
        if (infoElem) infoElem.textContent = infoText;
    }

    // Function to update pagination controls
    function updatePagination() {
        const pagination = document.getElementById('pagination');
        if (!pagination) return;
        pagination.innerHTML = '';

        // Always display pagination if there are rows, even if totalPages = 1
        if (totalRows === 0) {
            pagination.style.display = 'none';
            const infoElem = document.getElementById('paginationInfo');
            if (infoElem) infoElem.textContent = '';
            return;
        } else {
            pagination.style.display = 'flex';
        }

        // Previous button
        let prevClass = currentPage === 1 ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${currentPage - 1}" aria-label="Previous">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        // Page numbers with ellipsis
        let startPage = Math.max(1, currentPage - 2);
        let endPage = Math.min(totalPages, startPage + 4);

        if (endPage - startPage < 4) {
            startPage = Math.max(1, endPage - 4);
        }

        if (startPage > 1) {
            pagination.innerHTML += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="1">1</a>
                </li>
            `;
            if (startPage > 2) {
                pagination.innerHTML += `
                    <li class="page-item disabled">
                        <a class="page-link" href="#">...</a>
                    </li>
                `;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            let activeClass = i === currentPage ? 'active' : '';
            pagination.innerHTML += `
                <li class="page-item ${activeClass}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                pagination.innerHTML += `
                    <li class="page-item disabled">
                        <a class="page-link" href="#">...</a>
                    </li>
                `;
            }
            pagination.innerHTML += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
                </li>
            `;
        }

        // Next button
        let nextClass = currentPage === totalPages ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${nextClass}">
                <a class="page-link" href="#" data-page="${currentPage + 1}" aria-label="Next">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;
    }

    // Handle pagination click
    const pagination = document.getElementById('pagination');
    if (pagination) {
        pagination.addEventListener('click', function (e) {
            e.preventDefault();
            if (e.target.tagName === 'A' || e.target.tagName === 'I') {
                let target = e.target.tagName === 'I' ? e.target.parentElement : e.target;
                let page = parseInt(target.getAttribute('data-page'));
                if (page >= 1 && page <= totalPages) {
                    displayPage(page);
                }
            }
        });
    }

    // Initial display
    if (totalRows > 0) {
        displayPage(1);
    }

    // Xác nhận xóa
    let testFormToDelete = null;
    document.querySelectorAll('.btn-delete-test').forEach(function(btn) {
        btn.addEventListener('click', function() {
            testFormToDelete = document.getElementById(this.getAttribute('data-form-id'));
            var modal = new bootstrap.Modal(document.getElementById('deleteTestModal'));
            modal.show();
        });
    });
    const confirmBtn = document.getElementById('confirmDeleteTestBtn');
    if (confirmBtn) {
        confirmBtn.addEventListener('click', function() {
            if (testFormToDelete) {
                testFormToDelete.submit();
            }
        });
    }
});

