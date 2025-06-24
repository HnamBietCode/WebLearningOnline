$(document).ready(function () {
    // Automatically dismiss alerts after 5 seconds
    setTimeout(function () {
        $(".alert").fadeOut(500, function () {
            $(this).alert('close');
        });
    }, 5000);

    // Enhanced hover effect for table rows
    $(".table-row").hover(
        function () {
            $(this).addClass("row-hover");
        },
        function () {
            $(this).removeClass("row-hover");
        }
    );

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Pagination settings
    const rowsPerPage = 5; // Số bài tập mỗi trang
    let currentPage = 1;
    let rows = $("#assignmentTableBody tr");
    let totalRows = rows.length;
    let totalPages = Math.ceil(totalRows / rowsPerPage);

    // Function to display rows for the current page
    function displayPage(page) {
        currentPage = page;
        let start = (page - 1) * rowsPerPage;
        let end = start + rowsPerPage;

        rows.hide();
        rows.slice(start, end).show();

        updatePagination();
        updateAssignmentCount();
    }

    // Function to update pagination controls
    function updatePagination() {
        let pagination = $("#pagination");
        pagination.empty();

        // Previous button
        let prevClass = currentPage === 1 ? "disabled" : "";
        pagination.append(`
            <li class="page-item ${prevClass}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">
                    <i class="fas fa-chevron-left small"></i>
                </a>
            </li>
        `);

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            let activeClass = i === currentPage ? "active" : "";
            pagination.append(`
                <li class="page-item ${activeClass}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `);
        }

        // Next button
        let nextClass = currentPage === totalPages ? "disabled" : "";
        pagination.append(`
            <li class="page-item ${nextClass}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">
                    <i class="fas fa-chevron-right small"></i>
                </a>
            </li>
        `);
    }

    // Handle pagination click
    $("#pagination").on("click", "a", function (e) {
        e.preventDefault();
        let page = parseInt($(this).data("page"));
        if (page >= 1 && page <= totalPages) {
            displayPage(page);
        }
    });

    // Search functionality (chỉ tìm kiếm theo tiêu đề bài tập)
    $("#searchAssignment").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        rows = $("#assignmentTableBody tr").filter(function () {
            var title = $(this).find("td:eq(0) h6").text().toLowerCase();
            return title.indexOf(value) > -1;
        });
        totalRows = rows.length;
        totalPages = Math.ceil(totalRows / rowsPerPage);
        displayPage(1);
    });

    // Filter functionality
    $("#filterButton").click(function () {
        var courseValue = $("#courseFilter").val().toLowerCase();
        var typeValue = $("#typeFilter").val().toLowerCase();

        rows = $("#assignmentTableBody tr").filter(function () {
            var courseMatch = true;
            var typeMatch = true;

            if (courseValue !== "") {
                courseMatch = $(this).find("td:eq(1)").text().toLowerCase().indexOf(courseValue) > -1;
            }

            if (typeValue !== "") {
                typeMatch = $(this).find("td:eq(3)").text().toLowerCase().indexOf(typeValue) > -1;
            }

            return courseMatch && typeMatch;
        });

        totalRows = rows.length;
        totalPages = Math.ceil(totalRows / rowsPerPage);
        displayPage(1); 
    });

    // Update assignment count
    function updateAssignmentCount() {
        var visibleRowCount = $("#assignmentTableBody tr:visible").length;
        $("#assignmentCount").text(visibleRowCount + " bài tập");
    }

    // Sort functionality
    $(".sort-icon").click(function () {
        var $this = $(this);
        var column = $this.data("column");
        var asc = !$this.hasClass("asc");

        $(".sort-icon").removeClass("asc desc");

        if (asc) {
            $this.addClass("asc");
        } else {
            $this.addClass("desc");
        }

        sortTable(column, asc);
    });

    function sortTable(column, asc) {
        var rowsArray = rows.get();

        rowsArray.sort(function (a, b) {
            var A = $(a).children("td").eq(column).text().trim();
            var B = $(b).children("td").eq(column).text().trim();

            if (asc) {
                return A.localeCompare(B);
            } else {
                return B.localeCompare(A);
            }
        });

        $("#assignmentTableBody").empty();
        $.each(rowsArray, function (index, row) {
            $("#assignmentTableBody").append(row);
        });

        rows = $("#assignmentTableBody tr");
        totalRows = rows.length;
        totalPages = Math.ceil(totalRows / rowsPerPage);
        displayPage(1);
    }
});