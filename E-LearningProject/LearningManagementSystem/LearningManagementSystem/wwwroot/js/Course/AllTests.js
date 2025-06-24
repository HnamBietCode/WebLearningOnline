function searchTests() {
    const input = document.getElementById('searchTests');
    const filter = input.value.toUpperCase();
    const testCards = document.querySelectorAll('.test-card');

    testCards.forEach(card => {
        const titleElement = card.querySelector('.test-title');
        if (titleElement) {
            const title = titleElement.textContent || titleElement.innerText;
            if (title.toUpperCase().indexOf(filter) > -1) {
                card.style.display = "";
            } else {
                card.style.display = "none";
            }
        }
    });

    // Show/hide course sections based on visible test cards
    const courseSections = document.querySelectorAll('.course-section');
    courseSections.forEach(section => {
        const visibleTests = section.querySelectorAll('.test-card:not([style*="display: none"])').length;
        if (visibleTests === 0 && filter !== '') {
            section.style.display = "none";
        } else {
            section.style.display = "";
        }
    });
}

function showConfirmationModal(assignmentId, courseId, testTitle, actionType) {
    const modal = document.getElementById("confirmationModal");
    const modalTitle = document.getElementById("modalTitle");
    const modalMessage = document.getElementById("modalMessage");
    const confirmButton = document.getElementById("confirmButton");

    if (!modal || !modalTitle || !modalMessage || !confirmButton) {
        console.error("Modal elements not found");
        return;
    }

    modal.style.display = "flex";

    if (actionType === "view") {
        modalTitle.textContent = "Xem kết quả bài kiểm tra";
        modalMessage.textContent = `Bạn có muốn xem kết quả của bài kiểm tra "${testTitle}" không?`;
        confirmButton.onclick = function () {
            window.location.href = `/Course/ViewTestResult?assignmentId=${assignmentId}&id=${courseId}`;
        };
    } else {
        modalTitle.textContent = "Bắt đầu bài kiểm tra";
        modalMessage.textContent = `Bạn có muốn làm bài kiểm tra "${testTitle}" không?`;
        confirmButton.onclick = function () {
            window.location.href = `/Course/TakeTest?id=${courseId}&assignmentId=${assignmentId}`;
        };
    }

    document.getElementById("cancelButton").onclick = function () {
        modal.style.display = "none";
    };
}

// Close modal when clicking outside
window.onclick = function (event) {
    const modal = document.getElementById("confirmationModal");
    if (event.target === modal) {
        modal.style.display = "none";
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchTests');
    if (searchInput) {
        searchInput.addEventListener('input', searchTests);
    }
});