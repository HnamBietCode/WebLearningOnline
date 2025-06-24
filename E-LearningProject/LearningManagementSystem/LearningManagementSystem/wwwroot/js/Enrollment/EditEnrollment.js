$(document).ready(function () {
    // Enhance select dropdowns with select2 if available
    if ($.fn.select2 && $('#course-selector').length) {
        $('#course-selector').select2({
            placeholder: "Tìm kiếm và chọn khóa học",
            allowClear: true,
            theme: "bootstrap-5",
            width: "100%"
        });
    }
});