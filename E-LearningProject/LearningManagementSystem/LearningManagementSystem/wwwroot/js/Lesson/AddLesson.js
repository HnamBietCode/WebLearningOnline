$(document).ready(function () {
    // Focus vào trường tiêu đề khi trang load xong
    $('#lessonTitle').focus();

    // Xem trước tên file khi người dùng chọn file
    $('#videoFile').on('change', function () {
        let fileName = $(this).val().split('\\').pop();
        if (fileName) {
            $(this).next('.form-text').html('<i class="fas fa-check-circle text-success me-1"></i> Đã chọn: ' + fileName);
        }
    });

    // Cải thiện trải nghiệm cho textarea
    $('#lessonContent').on('focus', function () {
        $(this).parent().addClass('border border-primary');
    }).on('blur', function () {
        $(this).parent().removeClass('border border-primary');
    });
});