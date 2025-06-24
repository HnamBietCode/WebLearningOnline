// Bootstrap 5 validation
(function () {
    'use strict'

    // Fetch all forms we want to apply validation styles to
    var forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                const userName = document.querySelector('#UserName').value;
                const courseId = document.querySelector('#CourseId').value;

                if (!userName || !courseId || !form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                }

                form.classList.add('was-validated');
            }, false)
        })
})()

// Nâng cao trải nghiệm form với Select2
$(document).ready(function () {
    if ($.fn.select2) {
        $('#userSelect').select2({
            placeholder: "Tìm kiếm và chọn học viên",
            allowClear: true,
            theme: "bootstrap-5",
            width: "100%"
        });

        $('#courseSelect').select2({
            placeholder: "Tìm kiếm và chọn khóa học",
            allowClear: true,
            theme: "bootstrap-5",
            width: "100%"
        });
    }
});