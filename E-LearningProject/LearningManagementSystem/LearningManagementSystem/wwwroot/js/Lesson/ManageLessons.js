document.addEventListener("DOMContentLoaded", function () {
    // Kích hoạt tooltip
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Tìm kiếm bài học
    const searchInput = document.getElementById('searchInput');
    searchInput.addEventListener('keyup', function () {
        const filter = searchInput.value.toLowerCase();
        const rows = document.querySelectorAll('table tbody tr');

        rows.forEach(row => {
            const lessonTitle = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            if (lessonTitle.includes(filter)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    });

    // Xử lý modal xem video
    const playButtons = document.querySelectorAll('.play-button');
    playButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            const videoUrl = this.getAttribute('data-video');
            const modalVideo = document.getElementById('modalVideo');
            modalVideo.querySelector('source').src = videoUrl;
            modalVideo.load();

            const videoModal = new bootstrap.Modal(document.getElementById('videoModal'));
            videoModal.show();
        });
    });

    // Đóng và dừng video khi đóng modal
    const videoModal = document.getElementById('videoModal');
    videoModal.addEventListener('hidden.bs.modal', function () {
        const modalVideo = document.getElementById('modalVideo');
        modalVideo.pause();
    });
});

let lessonFormToDelete = null;
document.querySelectorAll('.btn-delete-lesson').forEach(function(btn) {
    btn.addEventListener('click', function() {
        lessonFormToDelete = document.getElementById(this.getAttribute('data-form-id'));
        var modal = new bootstrap.Modal(document.getElementById('deleteLessonModal'));
        modal.show();
    });
});
document.getElementById('confirmDeleteLessonBtn').addEventListener('click', function() {
    if (lessonFormToDelete) {
        lessonFormToDelete.submit();
    }
});