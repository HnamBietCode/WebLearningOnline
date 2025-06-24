// Tính năng xóa file video đã chọn
document.getElementById('clearVideoBtn').addEventListener('click', function () {
    document.getElementById('videoFileInput').value = '';
});

// Cải thiện trải nghiệm form
(function () {
    'use strict'
    var forms = document.querySelectorAll('.needs-validation')
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault()
                    event.stopPropagation()
                }
                form.classList.add('was-validated')
            }, false)
        })
})()

const form = document.querySelector('form');
const btnSave = document.getElementById('btnSaveLesson');
let isSubmitting = false;
let initialFormData = new FormData(form);

const lessonTitleInput = document.querySelector('input[name="LessonTitle"]');
const contentInput = document.querySelector('textarea[name="Content"]');
const videoInput = document.querySelector('input[name="videoFile"]');

if (lessonTitleInput && contentInput && btnSave) {
    const original = {
        title: lessonTitleInput.value,
        content: contentInput.value,
        video: videoInput ? videoInput.value : ''
    };
    function isChanged() {
        const titleNow = lessonTitleInput.value.trim();
        const contentNow = contentInput.value.trim();
        const videoNow = videoInput ? videoInput.value : '';
        const origTitle = original.title.trim();
        const origContent = original.content.trim();
        const origVideo = original.video;
        return titleNow !== origTitle || contentNow !== origContent || videoNow !== origVideo;
    }
    function checkChanged() {
        btnSave.disabled = !isChanged();
    }
    btnSave.disabled = true;
    lessonTitleInput.addEventListener('input', checkChanged);
    contentInput.addEventListener('input', checkChanged);
    if (videoInput) videoInput.addEventListener('input', checkChanged);
}

form.addEventListener('submit', function () {
    isSubmitting = true;
    btnSave.disabled = true;
    btnSave.innerText = 'Đang lưu...';
});