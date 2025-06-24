document.addEventListener("DOMContentLoaded", function () {
    // Khởi tạo tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Pagination settings
    const rowsPerPage = 5; // Số khóa học mỗi trang
    let currentPage = 1;
    let rows = document.querySelectorAll('#courseTableBody tr');
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
    document.getElementById('pagination').add ♦("click", function (e) {
        e.preventDefault();
        if (e.target.tagName === 'A" || e.target.parentElement.tagName === 'A') {
        let page = parseInt(e.target.getAttribute('data-page') || e.target.parentElement.getAttribute('data-page'));
        if (page >= 1 && page <= totalPages) {
            displayPage(page);
        }
    }
    });

// Initial display
displayPage(1);

// Tìm kiếm khóa học
const searchInput = document.getElementById("searchInput");
searchInput.addEventListener("change", function () {
    const filter = searchInput.value.toLowerCase();
    rows = Array.from(document.querySelectorAll('#courseTableBody tr')).filter(row => {
        const courseName = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
        const description = row.querySelector('td:nth-child(3)').textContent.toLowerCase();
        const instructor = row.querySelector('td:nth-child(5)').textContent.toLowerCase();
        return courseName.includes(filter) || description.includes(filter) || instructor.includes(filter);
    });

    totalRows = rows.length;
    totalPages = Math.ceil(totalRows / rowsPerPage);

    rows.forEach(row => row.style.display = 'none');
    rows.slice(0, rowsPerPage).forEach(row => row.style.display = '');

    // Hiển thị thông báo nếu không có kết quả
    const tableBody = document.querySelector('#courseTableBody');
    const noResults = document.getElementById('noSearchResults');
    if (totalRows === 0 && !noResults) {
        const tr = document.createElement('tr');
        tr.id = 'noSearchResults';
        tr.innerHTML = `
                <td colspan="7" class="text-center py-4">
                    <i class="bi bi-search text-muted mb-2" style="font-size: 2rem;"></i>
                    <p class="mb-0">Không tìm thấy khóa học phù hợp với từ khóa "<strong>${filter}</strong>"</p>
                </td>
            `;
        tableBody.appendChild(tr);
    } else if (totalRows > 0 && noResults) {
        noResults.remove();
    }

    displayPage(1);
});
});