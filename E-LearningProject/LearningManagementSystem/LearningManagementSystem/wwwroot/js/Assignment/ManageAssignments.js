document.addEventListener("DOMContentLoaded", function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Pagination settings
    const rowsPerPage = 5;
    let currentPage = 1;
    let rows = document.querySelectorAll('table tbody tr');
    let totalRows = rows.length;
    let totalPages = Math.ceil(totalRows / rowsPerPage);

    function displayPage(page) {
        currentPage = page;
        let start = (page - 1) * rowsPerPage;
        let end = start + rowsPerPage;

        rows.forEach((row, index) => {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        updatePagination();
    }

    function updatePagination() {
        let pagination = document.getElementById('pagination');
        pagination.innerHTML = '';

        let prevClass = currentPage === 1 ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${currentPage - 1}" aria-label="Previous">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        for (let i = 1; i <= totalPages; i++) {
            let activeClass = i === currentPage ? 'active' : '';
            pagination.innerHTML += `
                <li class="page-item ${activeClass}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
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

    document.getElementById('pagination').addEventListener('click', function (e) {
        e.preventDefault();
        if (e.target.tagName === 'A' || e.target.tagName === 'I') {
            let pageElement = e.target.tagName === 'I' ? e.target.parentElement : e.target;
            let page = parseInt(pageElement.getAttribute('data-page'));
            if (page >= 1 && page <= totalPages) {
                displayPage(page);
            }
        }
    });

    if (totalRows > 0) {
        displayPage(1);
    }

    const searchInput = document.getElementById('searchInput');
    searchInput.addEventListener('keyup', function () {
        const filter = searchInput.value.toLowerCase();
        rows = Array.from(document.querySelectorAll('table tbody tr')).filter(row => {
            const assignmentTitle = row.querySelector('td:nth-child(1)').textContent.toLowerCase();
            return assignmentTitle.includes(filter);
        });

        totalRows = rows.length;
        totalPages = Math.ceil(totalRows / rowsPerPage);

        rows.forEach(row => row.style.display = 'none');
        rows.slice(0, rowsPerPage).forEach(row => row.style.display = '');

        const tableBody = document.querySelector('table tbody');
        const noResults = document.getElementById('noSearchResults');
        if (totalRows === 0 && !noResults) {
            const tr = document.createElement('tr');
            tr.id = 'noSearchResults';
            tr.innerHTML = `
                <td colspan="4" class="text-center py-5">
                    <i class="bi bi-search text-muted mb-3" style="font-size: 2.5rem;"></i>
                    <p class="mb-0 h5">Không tìm thấy bài tập</p>
                    <p class="text-muted">Không có bài tập nào phù hợp với từ khóa "<strong>${filter}</strong>"</p>
                </td>
            `;
            tableBody.appendChild(tr);
        } else if (totalRows > 0 && noResults) {
            noResults.remove();
        }

        displayPage(1);
    });
});

let assignmentFormToDelete = null;
document.querySelectorAll('.btn-delete-assignment').forEach(function(btn) {
    btn.addEventListener('click', function() {
        assignmentFormToDelete = document.getElementById(this.getAttribute('data-form-id'));
        var modal = new bootstrap.Modal(document.getElementById('deleteAssignmentModal'));
        modal.show();
    });
});
document.getElementById('confirmDeleteAssignmentBtn').addEventListener('click', function() {
    if (assignmentFormToDelete) {
        assignmentFormToDelete.submit();
    }
});