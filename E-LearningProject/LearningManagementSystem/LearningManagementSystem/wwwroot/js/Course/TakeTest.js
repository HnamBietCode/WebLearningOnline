function initializeTest(durationInMinutes, totalQuestions, redirectUrl) {
    // Khai báo biến cho timer
    let totalSeconds = durationInMinutes * 60;
    let isSubmitting = false;
    let isTimeUp = false;
    let timerInterval;

    // Tính toán tiến độ làm bài
    function updateProgress() {
        const inputs = document.querySelectorAll('.question-input:checked, .question-input[type="textarea"]:not(:placeholder-shown)');
        const total = totalQuestions;
        const completed = inputs.length;

        document.getElementById('completedQuestions').textContent = completed;
        document.getElementById('progressBar').style.width = `${(completed / total) * 100}%`;

        // Cập nhật trạng thái nút điều hướng
        document.querySelectorAll('.question-input').forEach((input, index) => {
            if (input.checked || (input.tagName === 'TEXTAREA' && input.value.trim() !== '')) {
                const questionIndex = Array.from(document.querySelectorAll('.question-card')).findIndex(card => card.contains(input));
                const navButton = document.querySelector(`.nav-button[data-question="${questionIndex}"]`);
                if (navButton) {
                    navButton.classList.add('answered');
                }
            }
        });
    }

    // Hàm submit form
    async function submitForm() {
        if (isSubmitting) return;
        isSubmitting = true;

        const form = document.getElementById('testForm');
        const formData = new FormData(form);

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': form.querySelector('input[name="__RequestVerificationToken"]').value
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            // Hiển thị thông báo và chuyển hướng
            alert('Đã hết thời gian làm bài! Hệ thống sẽ tự động nộp bài của bạn.');
            window.location.href = redirectUrl;
        } catch (error) {
            console.error('Error during fetch:', error);
            alert("Đã có lỗi xảy ra khi lưu bài. Vui lòng thử lại.");
            window.location.href = redirectUrl;
        }
    }

    // Cập nhật timer
    function updateTimer() {
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        const timeDisplay = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

        document.getElementById('timer').textContent = timeDisplay;
        document.getElementById('timer-display').textContent = timeDisplay;

        if (totalSeconds <= 300) { // 5 phút cuối
            document.querySelector('.time-circle').style.background = 'linear-gradient(135deg, #ff9800, #f44336)';
        }

        if (totalSeconds <= 0 && !isTimeUp) {
            clearInterval(timerInterval);
            isTimeUp = true;

            // Vô hiệu hóa tất cả input
            const inputs = document.querySelectorAll('.question-input');
            inputs.forEach(input => input.disabled = true);
            document.getElementById('submitButton').disabled = true;

            // Submit form
            submitForm();
        } else {
            totalSeconds--;
        }
    }

    // Điều hướng câu hỏi
    function setupNavigation() {
        const navButtons = document.querySelectorAll('.nav-button');
        const questionCards = document.querySelectorAll('.question-card');

        navButtons.forEach(button => {
            button.addEventListener('click', () => {
                const questionIndex = button.getAttribute('data-question');
                if (questionCards[questionIndex]) {
                    questionCards[questionIndex].scrollIntoView({ behavior: 'smooth', block: 'start' });
                    navButtons.forEach(btn => btn.classList.remove('active'));
                    button.classList.add('active');
                }
            });
        });

        document.querySelectorAll('.question-input').forEach(input => {
            input.addEventListener('change', updateProgress);
            if (input.tagName === 'TEXTAREA') {
                input.addEventListener('input', updateProgress);
            }
        });
    }

    // Khởi tạo
    document.addEventListener('DOMContentLoaded', function () {
        // Khởi tạo timer
        updateTimer();
        timerInterval = setInterval(updateTimer, 1000);

        // Khởi tạo điều hướng
        setupNavigation();

        // Khởi tạo tiến độ
        updateProgress();

        // Xử lý submit form thủ công
        document.querySelector('#testForm').addEventListener('submit', function (e) {
            e.preventDefault();
            if (!isSubmitting) {
                submitForm();
            }
        });

        // Xử lý khi reload trang hoặc quay lại
        window.addEventListener('beforeunload', function (e) {
            if (!isSubmitting && totalSeconds > 0 && !isTimeUp) {
                // Lưu bài làm trước khi rời trang
                const form = document.getElementById('testForm');
                const formData = new FormData(form);
                fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'RequestVerificationToken': form.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });
                // Chỉ có thể dùng chuỗi cảnh báo mặc định
                e.preventDefault();
                e.returnValue = "Bạn đang rời khỏi trang làm bài. Hệ thống sẽ lưu bài và chuyển bạn về danh sách bài kiểm tra.";
                return e.returnValue;
            }
        });

        // Xử lý khi bấm nút quay lại (popstate)
        window.addEventListener('popstate', function (e) {
            if (!isSubmitting && totalSeconds > 0 && !isTimeUp) {
                const form = document.getElementById('testForm');
                const formData = new FormData(form);
                fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'RequestVerificationToken': form.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                }).then(() => {
                    Swal.fire({
                        title: 'Cảnh báo',
                        text: 'Bạn đang rời khỏi trang làm bài. Hệ thống sẽ lưu bài và chuyển bạn về danh sách bài kiểm tra.',
                        icon: 'warning',
                        confirmButtonText: 'OK',
                        allowOutsideClick: false,
                        allowEscapeKey: false
                    }).then(() => {
                        window.location.href = redirectUrl;
                    });
                }).catch(() => {
                    Swal.fire({
                        title: 'Lỗi',
                        text: 'Có lỗi xảy ra khi lưu bài làm. Vui lòng thử lại.',
                        icon: 'error',
                        confirmButtonText: 'OK',
                        allowOutsideClick: false,
                        allowEscapeKey: false
                    }).then(() => {
                        window.location.href = redirectUrl;
                    });
                });
            }
        });
    });
}