document.addEventListener('DOMContentLoaded', function () {
    // Slider functionality
    const slides = document.querySelectorAll('.slide');
    const dots = document.querySelectorAll('.dot');
    const prevBtn = document.querySelector('.prev-btn');
    const nextBtn = document.querySelector('.next-btn');
    const slidesContainer = document.querySelector('.slides');
    let currentIndex = 0;
    const slideInterval = 3000;

    function showSlide(index) {
        if (index >= slides.length) {
            currentIndex = 0;
        } else if (index < 0) {
            currentIndex = slides.length - 1;
        } else {
            currentIndex = index;
        }

        const translateValue = -currentIndex * 100 + '%';
        slidesContainer.style.transform = `translateX(${translateValue})`;

        dots.forEach(dot => dot.classList.remove('active'));
        dots[currentIndex].classList.add('active');
    }

    function autoSlide() {
        showSlide(currentIndex + 1);
    }

    showSlide(currentIndex);
    let slideTimer = setInterval(autoSlide, slideInterval);

    const slider = document.querySelector('.slider');
    slider.addEventListener('mouseenter', () => {
        clearInterval(slideTimer);
    });

    slider.addEventListener('mouseleave', () => {
        slideTimer = setInterval(autoSlide, slideInterval);
    });

    prevBtn.addEventListener('click', () => {
        showSlide(currentIndex - 1);
    });

    nextBtn.addEventListener('click', () => {
        showSlide(currentIndex + 1);
    });

    dots.forEach(dot => {
        dot.addEventListener('click', () => {
            const index = parseInt(dot.getAttribute('data-index'));
            showSlide(index);
        });
    });

    // Pagination for Suggested Courses
    const suggestedRowsPerPage = 4; // 4 khóa học mỗi trang
    let suggestedCurrentPage = 1;
    let suggestedRows = document.querySelectorAll('#suggestedCourses .course-item');
    let suggestedTotalRows = suggestedRows.length;
    let suggestedTotalPages = Math.ceil(suggestedTotalRows / suggestedRowsPerPage);

    function displaySuggestedPage(page) {
        suggestedCurrentPage = page;
        let start = (page - 1) * suggestedRowsPerPage;
        let end = Math.min(start + suggestedRowsPerPage, suggestedTotalRows);

        suggestedRows.forEach((row, index) => {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        updateSuggestedPagination();
        updateSuggestedPaginationInfo();
    }

    function updateSuggestedPaginationInfo() {
        let start = (suggestedCurrentPage - 1) * suggestedRowsPerPage + 1;
        let end = Math.min(suggestedCurrentPage * suggestedRowsPerPage, suggestedTotalRows);
        let infoText = suggestedTotalRows > 0 ? `Hiển thị ${start}-${end} trên ${suggestedTotalRows} khóa học` : '';
        document.getElementById('suggestedPaginationInfo').textContent = infoText;
    }

    function updateSuggestedPagination() {
        let pagination = document.getElementById('suggestedPagination');
        pagination.innerHTML = '';

        if (suggestedTotalRows === 0) {
            pagination.style.display = 'none';
            document.getElementById('suggestedPaginationInfo').textContent = '';
            return;
        } else {
            pagination.style.display = 'flex';
        }

        let prevClass = suggestedCurrentPage === 1 ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${suggestedCurrentPage - 1}" aria-label="Previous">
                    <i class="fas fa-chevron-left"></i>
                </a>
            </li>
        `;

        let startPage = Math.max(1, suggestedCurrentPage - 2);
        let endPage = Math.min(suggestedTotalPages, startPage + 4);

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
            let activeClass = i === suggestedCurrentPage ? 'active' : '';
            pagination.innerHTML += `
                <li class="page-item ${activeClass}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        if (endPage < suggestedTotalPages) {
            if (endPage < suggestedTotalPages - 1) {
                pagination.innerHTML += `
                    <li class="page-item disabled">
                        <a class="page-link" href="#">...</a>
                    </li>
                `;
            }
            pagination.innerHTML += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${suggestedTotalPages}">${suggestedTotalPages}</a>
                </li>
            `;
        }

        let nextClass = suggestedCurrentPage === suggestedTotalPages ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${nextClass}">
                <a class="page-link" href="#" data-page="${suggestedCurrentPage + 1}" aria-label="Next">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;
    }

    document.getElementById('suggestedPagination').addEventListener('click', function (e) {
        e.preventDefault();
        if (e.target.tagName === 'A' || e.target.tagName === 'I') {
            let target = e.target.tagName === 'I' ? e.target.parentElement : e.target;
            let page = parseInt(target.getAttribute('data-page'));
            if (page >= 1 && page <= suggestedTotalPages) {
                displaySuggestedPage(page);
            }
        }
    });

    if (suggestedTotalRows > 0) {
        displaySuggestedPage(1);
    }

    // Pagination for Enrolled Courses
    const enrolledRowsPerPage = 4; // 4 khóa học mỗi trang
    let enrolledCurrentPage = 1;
    let enrolledRows = document.querySelectorAll('#enrolledCourses .course-item');
    let enrolledTotalRows = enrolledRows.length;
    let enrolledTotalPages = Math.ceil(enrolledTotalRows / enrolledRowsPerPage);

    function displayEnrolledPage(page) {
        enrolledCurrentPage = page;
        let start = (page - 1) * enrolledRowsPerPage;
        let end = Math.min(start + enrolledRowsPerPage, enrolledTotalRows);

        enrolledRows.forEach((row, index) => {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        updateEnrolledPagination();
        updateEnrolledPaginationInfo();
    }

    function updateEnrolledPaginationInfo() {
        let start = (enrolledCurrentPage - 1) * enrolledRowsPerPage + 1;
        let end = Math.min(enrolledCurrentPage * enrolledRowsPerPage, enrolledTotalRows);
        let infoText = enrolledTotalRows > 0 ? `Hiển thị ${start}-${end} trên ${enrolledTotalRows} khóa học` : '';
        document.getElementById('enrolledPaginationInfo').textContent = infoText;
    }

    function updateEnrolledPagination() {
        let pagination = document.getElementById('enrolledPagination');
        pagination.innerHTML = '';

        if (enrolledTotalRows === 0) {
            pagination.style.display = 'none';
            document.getElementById('enrolledPaginationInfo').textContent = '';
            return;
        } else {
            pagination.style.display = 'flex';
        }

        let prevClass = enrolledCurrentPage === 1 ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${enrolledCurrentPage - 1}" aria-label="Previous">
                    <i class="fas fa-chevron-left"></i>
                </a>
            </li>
        `;

        let startPage = Math.max(1, enrolledCurrentPage - 2);
        let endPage = Math.min(enrolledTotalPages, startPage + 4);

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
            let activeClass = i === enrolledCurrentPage ? 'active' : '';
            pagination.innerHTML += `
                <li class="page-item ${activeClass}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        if (endPage < enrolledTotalPages) {
            if (endPage < enrolledTotalPages - 1) {
                pagination.innerHTML += `
                    <li class="page-item disabled">
                        <a class="page-link" href="#">...</a>
                    </li>
                `;
            }
            pagination.innerHTML += `
                <li class="page-item">
                    <a class="page-link" href="#" data-page="${enrolledTotalPages}">${enrolledTotalPages}</a>
                </li>
            `;
        }

        let nextClass = enrolledCurrentPage === enrolledTotalPages ? 'disabled' : '';
        pagination.innerHTML += `
            <li class="page-item ${nextClass}">
                <a class="page-link" href="#" data-page="${enrolledCurrentPage + 1}" aria-label="Next">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;
    }

    document.getElementById('enrolledPagination').addEventListener('click', function (e) {
        e.preventDefault();
        if (e.target.tagName === 'A' || e.target.tagName === 'I') {
            let target = e.target.tagName === 'I' ? e.target.parentElement : e.target;
            let page = parseInt(target.getAttribute('data-page'));
            if (page >= 1 && page <= enrolledTotalPages) {
                displayEnrolledPage(page);
            }
        }
    });

    if (enrolledTotalRows > 0) {
        displayEnrolledPage(1);
    }

    // Update pagination when switching tabs
    document.getElementById('courseTabs').addEventListener('shown.bs.tab', function (e) {
        if (e.target.id === 'suggested-tab') {
            if (suggestedTotalRows > 0) {
                displaySuggestedPage(suggestedCurrentPage);
            }
        } else if (e.target.id === 'enrolled-tab') {
            if (enrolledTotalRows > 0) {
                displayEnrolledPage(enrolledCurrentPage);
            }
        }
    });
});