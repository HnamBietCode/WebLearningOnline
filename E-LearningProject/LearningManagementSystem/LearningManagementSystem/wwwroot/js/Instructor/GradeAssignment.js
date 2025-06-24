$(document).ready(function () {
    // Client-side validation
    $('form').on('submit', function (e) {
        const scoreInput = $(this).find('input[name="score"]');
        const score = parseFloat(scoreInput.val());
        const maxScore = parseFloat(scoreInput.attr('max'));

        if (isNaN(score) || score < 0 || score > maxScore) {
            e.preventDefault();
            Swal.fire({
                icon: 'error',
                title: 'Lỗi nhập điểm',
                text: `Điểm phải nằm trong khoảng từ 0 đến ${maxScore}`,
                confirmButtonColor: '#4361ee',
                confirmButtonText: 'Đã hiểu'
            });
        }
    });

    // Add visual feedback when score changes
    $('input[name="score"]').on('change', function () {
        $(this).addClass('bg-primary-soft');
        setTimeout(() => {
            $(this).removeClass('bg-primary-soft');
        }, 800);
    });

    // Auto resize textarea as user types
    $('textarea').each(function () {
        this.setAttribute('style', 'height:' + (this.scrollHeight) + 'px;overflow-y:hidden;');
    }).on('input', function () {
        this.style.height = 'auto';
        this.style.height = (this.scrollHeight) + 'px';
    });

    // Make accordion items stand out on hover
    $('.accordion-item').hover(
        function () { $(this).addClass('shadow'); },
        function () { $(this).removeClass('shadow'); }
    );

    // Make tabs more interactive
    $('.nav-link').hover(
        function () { $(this).addClass('shadow-sm'); },
        function () { $(this).removeClass('shadow-sm'); }
    );

    // --- Disable nút Lưu nếu không thay đổi hoặc không hợp lệ ---
    $('form').each(function () {
        var $form = $(this);
        var $score = $form.find('input[name="score"]');
        var $feedback = $form.find('textarea[name="feedback"]');
        var $btn = $form.find('button[type="submit"]');
        var $originalScore = $form.find('.original-score');
        var $originalFeedback = $form.find('.original-feedback');
        var originalScore = ($originalScore.val() || '').replace(',', '.').trim();
        var originalFeedback = ($originalFeedback.val() || '').trim();
        var maxScore = parseFloat($score.attr('max'));

        function isValid() {
            var scoreVal = $score.val().replace(',', '.').trim();
            var feedbackVal = $feedback.val().trim();
            var scoreNum = parseFloat(scoreVal);
            if (scoreVal === '' || isNaN(scoreNum) || scoreNum < 0 || scoreNum > maxScore) return false;
            if (!feedbackVal) return false;
            return true;
        }

        function checkChangedAndValid() {
            var scoreVal = $score.val().replace(',', '.').trim();
            var feedbackVal = $feedback.val().trim();
            var changed = (scoreVal !== originalScore) || (feedbackVal !== originalFeedback);
            if (changed && isValid()) {
                $btn.prop('disabled', false);
            } else {
                $btn.prop('disabled', true);
            }
        }
        $btn.prop('disabled', true);
        $score.on('input', checkChangedAndValid);
        $feedback.on('input', checkChangedAndValid);
        checkChangedAndValid();
    });
});

