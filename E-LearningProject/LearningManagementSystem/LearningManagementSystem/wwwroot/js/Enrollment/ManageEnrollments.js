document.addEventListener("DOMContentLoaded", function () {
    // Khởi tạo tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Search functionality
    const searchInput = document.getElementById('searchInput');
    const clearSearchBtn = document.getElementById('clearSearch');
    const enrollmentTable = document.getElementById('enrollmentTable');
    const enrollmentCount = document.getElementById('enrollmentCount');
    const rows = enrollmentTable ? enrollmentTable.querySelectorAll('tbody tr') : [];

    function performSearch() {
        const searchTerm = searchInput.value.toLowerCase();
        let visibleCount = 0;

        rows.forEach(row => {
            const studentName = row.querySelector('td:nth-child(1)').textContent.toLowerCase();
            const courseName = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            const enrollmentDate = row.querySelector('td:nth-child(3)').textContent.toLowerCase();

            if (studentName.includes(searchTerm) || 
                courseName.includes(searchTerm) || 
                enrollmentDate.includes(searchTerm)) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        enrollmentCount.textContent = visibleCount;
    }

    if (searchInput) {
        searchInput.addEventListener('input', performSearch);
    }

    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', function() {
            searchInput.value = '';
            performSearch();
        });
    }

    // Hiệu ứng hover cho các hàng
    const tableRows = document.querySelectorAll('#enrollmentTableBody tr');
    tableRows.forEach(row => {
        row.addEventListener('mouseover', function () {
            this.classList.add('bg-light');
        });

        row.addEventListener('mouseout', function () {
            this.classList.remove('bg-light');
        });
    });
});

let enrollmentFormToDelete = null;
        document.querySelectorAll('.btn-delete-enrollment').forEach(function(btn) {
            btn.addEventListener('click', function() {
                enrollmentFormToDelete = document.getElementById(this.getAttribute('data-form-id'));
                var modal = new bootstrap.Modal(document.getElementById('deleteEnrollmentModal'));
                modal.show();
            });
        });
        document.getElementById('confirmDeleteEnrollmentBtn').addEventListener('click', function() {
            if (enrollmentFormToDelete) {
                enrollmentFormToDelete.submit();
            }
        });

        

        