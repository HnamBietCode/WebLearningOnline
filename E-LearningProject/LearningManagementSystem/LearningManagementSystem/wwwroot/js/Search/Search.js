document.addEventListener('DOMContentLoaded', function () {
    const rowsPerPage = 8; // Hiển thị 8 khóa học mỗi trang
    let currentPage = 1;
    let rows = document.querySelectorAll('#course-list .course-item');
    let totalRows = rows.length;
    let totalPages = Math.ceil(totalRows / rowsPerPage);

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

    function updatePaginationInfo() {
        let start = (currentPage - 1) * rowsPerPage + 1;
        let end = Math.min(currentPage * rowsPerPage, totalRows);
        let infoText = totalRows > 0 ? `Hiển thị ${start}-${end} trên ${totalRows} khóa học` : '';
        document.getElementById('paginationInfo').textContent = infoText;
    }

    function updatePagination() {
        let pagination = document.getElementById('coursePagination');
        pagination.innerHTML = '';

        if (totalRows === 0) {
            pagination.style.display = 'none';
            document.getElementById('paginationInfo').textContent = '';
            return;
        } else {
            pagination.style.display = 'flex';
        }

        let prevClass = currentPage === 1 ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${currentPage - 1}" aria-label="Previous">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

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
                        <span class="page-link">...</span>
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
                        <span class="page-link">...</span>
                    </li>
                `;
            }
            pagination.innerHTML += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
                </li>
            `;
        }

        let nextClass = currentPage === totalPages ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${nextClass}">
                <a class="page-link" href="#" data-page="${currentPage + 1}" aria-label="Next">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;
    }

    document.getElementById('coursePagination').addEventListener('click', function (e) {
        e.preventDefault();
        if (e.target.tagName.toLowerCase() === 'a' || e.target.tagName.toLowerCase() === 'i') {
            let target = e.target.tagName.toLowerCase() === 'i' ? e.target.parentElement : e.target;
            let page = parseInt(target.dataset.page);
            if (page >= 1 && page <= totalPages) {
                displayPage(page);
                window.scrollTo({ top: document.querySelector('#course-list').offsetTop - 100, behavior: 'smooth' });
            }
        }
    });

    if (totalRows > 0) {
        displayPage(1);
    }
});