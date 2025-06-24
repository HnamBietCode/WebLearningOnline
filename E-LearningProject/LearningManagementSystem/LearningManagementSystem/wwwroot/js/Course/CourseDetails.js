document.addEventListener('DOMContentLoaded', function () {
    // Hiệu ứng hiển thị khi tải trang
    window.addEventListener('DOMContentLoaded', () => {
        document.querySelector('.course-info-card')?.classList.add('show');
        document.querySelector('.course-image-card')?.classList.add('show');
        document.querySelector('.progress-card')?.classList.add('show');
        document.querySelector('.comment-form-card')?.classList.add('show');
    });

    // Hàm ẩn lời nhắc đăng ký
    function hideEnrollPrompt(courseId) {
        const enrollPrompt = document.querySelector(`#enrollForm-${courseId}`)?.parentElement;
        if (enrollPrompt) {
            enrollPrompt.style.display = 'none';
        }
    }

    // Xử lý sự kiện video
    const videos = document.querySelectorAll('.custom-video');
    if (!videos || videos.length === 0) {
        console.warn('Không tìm thấy video nào với class "custom-video".');
        return;
    }

    videos.forEach(video => {
        video.addEventListener('play', function () {
            const lessonId = this.getAttribute('data-lesson-id');
            if (!lessonId) {
                console.error('LessonId không được định nghĩa cho video:', this);
                return;
            }
            console.log('Video bắt đầu phát. LessonId:', lessonId);
            updateProgress(lessonId, false); // Đánh dấu trạng thái "đang tiến hành"
        });

        video.addEventListener('ended', function () {
            const lessonId = this.getAttribute('data-lesson-id');
            if (!lessonId) {
                console.error('LessonId không được định nghĩa cho video:', this);
                return;
            }
            console.log('Video kết thúc. LessonId:', lessonId);
            updateProgress(lessonId, true); // Đánh dấu trạng thái "hoàn thành"
        });
    });

    // Hàm cập nhật tiến trình học tập
    function updateProgress(lessonId, completionStatus) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.error('Không tìm thấy token chống giả mạo. Vui lòng kiểm tra form chứa token.');
            alert('Không thể cập nhật tiến trình: Token chống giả mạo không được tìm thấy.');
            return;
        }

        const formData = new FormData();
        formData.append('lessonId', lessonId);
        formData.append('completionStatus', completionStatus.toString());
        formData.append('__RequestVerificationToken', token);

        fetch('/Progress/UpdateProgress', {
            method: 'POST',
            body: formData
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Lỗi HTTP! Trạng thái: ${response.status} - ${response.statusText}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('Phản hồi từ server:', data);
                if (data.success) { // Giả định server trả về { success: true/false, message: "..." }
                    // Cập nhật trạng thái icon
                    const icon = document.querySelector(`.completion-icon[data-lesson-id="${lessonId}"]`);
                    if (icon) {
                        if (completionStatus) {
                            icon.classList.remove('in-progress');
                            icon.classList.add('completed');
                        } else {
                            icon.classList.add('in-progress');
                            icon.classList.remove('completed');
                        }
                    } else {
                        console.warn(`Không tìm thấy icon hoàn thành cho lessonId: ${lessonId}`);
                    }

                    // Cập nhật trạng thái lesson card
                    const lessonCard = document.querySelector(`.lesson-card:has(.completion-icon[data-lesson-id="${lessonId}"])`);
                    if (lessonCard) {
                        if (completionStatus) {
                            lessonCard.classList.remove('in-progress');
                            lessonCard.classList.add('completed');
                        } else {
                            lessonCard.classList.add('in-progress');
                            lessonCard.classList.remove('completed');
                        }
                    } else {
                        console.warn(`Không tìm thấy lesson card cho lessonId: ${lessonId}`);
                    }

                    // Cập nhật phần Progress Section (nếu cần)
                    updateProgressSection();
                } else {
                    console.error('Cập nhật tiến trình thất bại:', data.message || 'Không có thông báo lỗi từ server.');
                    alert('Cập nhật tiến trình thất bại: ' + (data.message || 'Lỗi không xác định.'));
                }
            })
            .catch(error => {
                console.error('Lỗi khi gửi yêu cầu cập nhật tiến trình:', error);
                alert('Đã xảy ra lỗi khi cập nhật tiến trình: ' + error.message);
            });
    }

    // Hàm cập nhật phần Progress Section
    function updateProgressSection() {
        // Assume courseId is passed dynamically, e.g., via a data attribute
        const courseId = document.querySelector('.course-details-container')?.dataset.courseId;
        if (!courseId) {
            console.error('courseId không được định nghĩa.');
            return;
        }

        fetch(`/Course/GetProgress?courseId=${courseId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Lỗi HTTP! Trạng thái: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('Dữ liệu tiến trình mới:', data);
                if (data && typeof data.progressPercent !== 'undefined') {
                    const progressCircle = document.querySelector('.progress-circle circle:nth-child(2)');
                    const percentageText = document.querySelector('.percentage');
                    const completedLessonsText = document.querySelector('.stat-item:nth-child(1) .stat-value');
                    const remainingLessonsText = document.querySelector('.stat-item:nth-child(2) .stat-value');

                    if (progressCircle && percentageText && completedLessonsText && remainingLessonsText) {
                        progressCircle.setAttribute('stroke-dasharray', `${data.progressPercent}, 100`);
                        percentageText.textContent = `${data.progressPercent}%`;
                        completedLessonsText.textContent = data.completedLessons || 0;
                        remainingLessonsText.textContent = data.remainingLessons || 0;
                    }
                }
            })
            .catch(error => {
                console.error('Lỗi khi cập nhật Progress Section:', error);
            });
    }

    // Xử lý chọn sao trong form bình luận
    const starRating = document.getElementById('commentRating');
    if (starRating) {
        const stars = starRating.querySelectorAll('input[type="radio"]');
        const labels = starRating.querySelectorAll('label');

        function highlightStars(value) {
            labels.forEach((label, index) => {
                const starValue = (5 - index).toString();
                label.style.color = starValue <= value ? '#fbbc05' : '#ddd';
            });
        }

        stars.forEach(star => {
            star.addEventListener('change', function () {
                const value = this.value;
                highlightStars(value);
                console.log('Selected rating:', value);
            });
        });

        labels.forEach(label => {
            label.addEventListener('click', function () {
                const value = this.getAttribute('for').replace('star', '');
                const correspondingInput = document.getElementById(this.getAttribute('for'));
                if (correspondingInput) {
                    correspondingInput.checked = true;
                    highlightStars(value);
                    console.log('Label clicked, selected rating:', value);
                }
            });

            label.addEventListener('mouseover', function () {
                const value = this.getAttribute('for').replace('star', '');
                highlightStars(value);
            });

            label.addEventListener('mouseout', function () {
                const checkedValue = starRating.querySelector('input[type="radio"]:checked')?.value || 0;
                highlightStars(checkedValue);
            });

            label.addEventListener('touchstart', function () {
                const value = this.getAttribute('for').replace('star', '');
                const correspondingInput = document.getElementById(this.getAttribute('for'));
                if (correspondingInput) {
                    correspondingInput.checked = true;
                    highlightStars(value);
                    console.log('Touchstart, selected rating:', value);
                }
            });
        });

        const checkedStar = starRating.querySelector('input[type="radio"]:checked');
        if (checkedStar) {
            highlightStars(checkedStar.value);
            console.log('Initial rating:', checkedStar.value);
        } else {
            highlightStars('0');
        }
    } else {
        console.warn('Không tìm thấy phần đánh giá sao với id="commentRating".');
    }

    // Xử lý sự kiện nhấn nút "Chỉnh sửa" bình luận
    const editButtons = document.querySelectorAll('.btn-edit');
    editButtons.forEach(button => {
        button.addEventListener('click', function () {
            const commentId = this.getAttribute('data-comment-id');
            const commentCard = document.querySelector(`.comment-card[data-comment-id="${commentId}"]`);
            const commentContent = commentCard.querySelector('.comment-content');
            const commentText = commentContent.querySelector('p');
            const courseId = document.querySelector('.course-details-container')?.dataset.courseId;
            if (!courseId) {
                console.error('courseId không được định nghĩa.');
                return;
            }
            const editForm = document.createElement('form');
            editForm.className = 'edit-comment-form';
            editForm.setAttribute('data-comment-id', commentId);
            editForm.innerHTML = `
                <input type="hidden" name="id" value="${courseId}" />
                <input type="hidden" name="commentId" value="${commentId}" />
                <div class="mb-3">
                    <label class="form-label fw-bold">Đánh giá khóa học</label>
                    <div class="star-rating">
                        <input type="radio" id="edit-star5-${commentId}" name="rating" value="5" required />
                        <label for="edit-star5-${commentId}" title="5 sao"><i class="bi bi-star-fill"></i></label>
                        <input type="radio" id="edit-star4-${commentId}" name="rating" value="4" />
                        <label for="edit-star4-${commentId}" title="4 sao"><i class="bi bi-star-fill"></i></label>
                        <input type="radio" id="edit-star3-${commentId}" name="rating" value="3" />
                        <label for="edit-star3-${commentId}" title="3 sao"><i class="bi bi-star-fill"></i></label>
                        <input type="radio" id="edit-star2-${commentId}" name="rating" value="2" />
                        <label for="edit-star2-${commentId}" title="2 sao"><i class="bi bi-star-fill"></i></label>
                        <input type="radio" id="edit-star1-${commentId}" name="rating" value="1" />
                        <label for="edit-star1-${commentId}" title="1 sao"><i class="bi bi-star-fill"></i></label>
                    </div>
                </div>
                <div class="mb-3">
                    <textarea class="form-control" name="content" rows="4" required>${commentText.textContent}</textarea>
                </div>
                <div class="d-flex gap-2">
                    <button type="submit" class="btn btn-success">Lưu</button>
                    <button type="button" class="btn btn-secondary cancel-edit-btn">Hủy</button>
                </div>
            `;
            commentContent.appendChild(editForm);
            commentText.style.display = 'none';
            commentCard.querySelector('.comment-actions').style.display = 'none';
            editForm.style.display = 'block';

            const editStarRating = editForm.querySelector('.star-rating');
            if (editStarRating) {
                const editStars = editStarRating.querySelectorAll('input[type="radio"]');
                const editLabels = editStarRating.querySelectorAll('label');

                function highlightEditStars(value) {
                    editLabels.forEach((label, index) => {
                        const starValue = (5 - index).toString();
                        label.style.color = starValue <= value ? '#fbbc05' : '#ddd';
                    });
                }

                editStars.forEach(star => {
                    star.addEventListener('change', function () {
                        highlightEditStars(this.value);
                        console.log('Edit form selected rating:', this.value);
                    });
                });

                editLabels.forEach(label => {
                    label.addEventListener('click', function () {
                        const value = this.getAttribute('for').replace(`edit-star`, '').replace(`-${commentId}`, '');
                        const correspondingInput = document.getElementById(this.getAttribute('for'));
                        if (correspondingInput) {
                            correspondingInput.checked = true;
                            highlightEditStars(value);
                            console.log('Edit form label clicked, selected rating:', value);
                        }
                    });

                    label.addEventListener('mouseover', function () {
                        const value = this.getAttribute('for').replace(`edit-star`, '').replace(`-${commentId}`, '');
                        highlightEditStars(value);
                    });

                    label.addEventListener('mouseout', function () {
                        const checkedValue = editStarRating.querySelector('input[type="radio"]:checked')?.value || 0;
                        highlightEditStars(checkedValue);
                    });
                });

                const checkedEditStar = editStarRating.querySelector('input[type="radio"]:checked');
                if (checkedEditStar) {
                    highlightEditStars(checkedEditStar.value);
                } else {
                    highlightEditStars('0');
                }
            }
        });
    });

    // Xử lý sự kiện nhấn nút "Hủy" trong form chỉnh sửa
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('cancel-edit-btn')) {
            const editForm = e.target.closest('.edit-comment-form');
            const commentId = editForm.getAttribute('data-comment-id');
            const commentCard = document.querySelector(`.comment-card[data-comment-id="${commentId}"]`);
            const commentContent = commentCard.querySelector('.comment-content');
            const commentText = commentContent.querySelector('p');
            commentText.style.display = 'block';
            commentCard.querySelector('.comment-actions').style.display = 'flex';
            editForm.remove();
        }
    });

    // Xử lý sự kiện gửi form chỉnh sửa
    document.addEventListener('submit', function (e) {
        if (e.target.classList.contains('edit-comment-form')) {
            e.preventDefault();
            const formData = new FormData(e.target);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!token) {
                console.error('Không tìm thấy token chống giả mạo.');
                alert('Không tìm thấy token chống giả mạo. Vui lòng tải lại trang.');
                return;
            }
            formData.append('__RequestVerificationToken', token);

            fetch('/Course/EditComment', {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`Lỗi HTTP! Trạng thái: ${response.status}`);
                    }
                    return response.text();
                })
                .then(data => {
                    window.location.reload();
                })
                .catch(error => {
                    console.error('Lỗi khi chỉnh sửa bình luận:', error);
                    alert('Đã xảy ra lỗi khi chỉnh sửa bình luận: ' + error.message);
                });
        }
    });

    // Xử lý sự kiện nhấn nút "Xóa" bình luận
    const deleteButtons = document.querySelectorAll('.btn-delete');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function () {
            const commentId = this.getAttribute('data-comment-id');
            const courseId = document.querySelector('.course-details-container')?.dataset.courseId;
            if (commentId && courseId) {
                if (confirm('Bạn có chắc chắn muốn xóa bình luận này không?')) {
                    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                    if (!token) {
                        console.error('Không tìm thấy token chống giả mạo.');
                        alert('Không tìm thấy token chống giả mạo. Vui lòng tải lại trang.');
                        return;
                    }

                    const formData = new FormData();
                    formData.append('id', courseId);
                    formData.append('commentId', commentId);
                    formData.append('__RequestVerificationToken', token);

                    fetch('/Course/DeleteComment', {
                        method: 'POST',
                        body: formData
                    })
                        .then(response => {
                            if (!response.ok) {
                                throw new Error(`Lỗi HTTP! Trạng thái: ${response.status}`);
                            }
                            return response.text();
                        })
                        .then(data => {
                            window.location.reload();
                        })
                        .catch(error => {
                            console.error('Lỗi khi xóa bình luận:', error);
                            alert('Đã xảy ra lỗi khi xóa bình luận: ' + error.message);
                        });
                }
            } else {
                console.error('courseId hoặc commentId không hợp lệ:', { courseId, commentId });
                alert('Không thể xóa bình luận. Dữ liệu không hợp lệ.');
            }
        });
    });

    // Xử lý sự kiện gửi form bài tập
    const assignmentForms = document.querySelectorAll('.assignment-form');
    assignmentForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(form);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!token) {
                console.error('Không tìm thấy token chống giả mạo.');
                alert('Không tìm thấy token chống giả mạo. Vui lòng tải lại trang.');
                return;
            }
            formData.append('__RequestVerificationToken', token);

            fetch(form.action, {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`Lỗi HTTP! Trạng thái: ${response.status}`);
                    }
                    return response.text();
                })
                .then(data => {
                    window.location.reload();
                })
                .catch(error => {
                    console.error('Lỗi khi nộp bài tập:', error);
                    alert('Đã xảy ra lỗi khi nộp bài tập: ' + error.message);
                });
        });
    });
});