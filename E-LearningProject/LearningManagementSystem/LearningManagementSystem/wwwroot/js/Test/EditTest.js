document.addEventListener("DOMContentLoaded", function () {
    const questionsContainer = document.getElementById("questionsContainer");
    const totalScoreDisplay = document.getElementById("scoreProgressBar");
    const addQuestionButton = document.querySelector(".add-question");

    // Hàm cập nhật hiển thị tổng điểm và kiểm tra nút thêm câu hỏi
    function updateTotalScoreDisplay() {
        const maxScoreInputs = document.querySelectorAll(".max-score");
        let totalScore = 0;
        maxScoreInputs.forEach(input => {
            const score = parseFloat(input.value) || 0;
            totalScore += score;
        });

        const percentage = Math.min(totalScore * 10, 100);
        totalScoreDisplay.style.width = percentage + "%";
        totalScoreDisplay.setAttribute("aria-valuenow", totalScore);
        totalScoreDisplay.textContent = totalScore + "/10 điểm";

        if (totalScore === 10) {
            totalScoreDisplay.classList.remove("bg-danger", "bg-warning");
            totalScoreDisplay.classList.add("bg-success");
        } else if (totalScore > 10) {
            totalScoreDisplay.classList.remove("bg-success", "bg-warning");
            totalScoreDisplay.classList.add("bg-danger");
        } else {
            totalScoreDisplay.classList.remove("bg-success", "bg-danger");
            totalScoreDisplay.classList.add("bg-warning");
        }

        if (totalScore >= 10) {
            addQuestionButton.disabled = true;
            addQuestionButton.classList.remove("btn-success");
            addQuestionButton.classList.add("btn-secondary");
        } else {
            addQuestionButton.disabled = false;
            addQuestionButton.classList.remove("btn-secondary");
            addQuestionButton.classList.add("btn-success");
        }
    }

    // Hàm kiểm tra tổng điểm tối đa
    window.validateTotalScore = function () {
        const maxScoreInputs = document.querySelectorAll(".max-score");
        let totalScore = 0;
        maxScoreInputs.forEach(input => {
            const score = parseFloat(input.value) || 0;
            totalScore += score;
        });

        updateTotalScoreDisplay();

        if (Math.abs(totalScore - 10) > 0.001) {
            alert("Tổng điểm tối đa của tất cả câu hỏi phải bằng 10!");
            return false;
        }
        return true;
    };

    document.addEventListener("input", function (e) {
        if (e.target.classList.contains("max-score")) {
            updateTotalScoreDisplay();
        }
    });

    function updateQuestionUI(item, index) {
        const questionTypeSelect = item.querySelector(".question-type");
        const isMultipleChoice = questionTypeSelect.value === "MultipleChoice";
        let optionsContainer = item.querySelector(".options-container");

        if (isMultipleChoice) {
            if (!optionsContainer) {
                optionsContainer = document.createElement("div");
                optionsContainer.className = "options-container mt-4";
                optionsContainer.innerHTML = `
                    <label class="form-label fw-bold mb-3">Danh sách đáp án <span class="text-danger">*</span></label>
                    <div class="options-list">
                        <div class="option-item mb-3" data-option-index="0">
                            <input type="hidden" name="Questions[${index}].Options[0].OptionId" value="" />
                            <input type="hidden" name="Questions[${index}].Options[0].QuestionId" value="" />
                            <input type="hidden" name="Questions[${index}].Options[0].IsCorrect" value="true" class="is-correct-hidden" />
                            <div class="input-group">
                                <div class="input-group-text">
                                    <input type="radio" name="Questions[${index}].CorrectOptionIndex" value="0" class="form-check-input correct-option required checked />
                                </div>
                                <input type="text" name="Questions[${index}].Options[0].OptionText" class="form-control option-text" placeholder="Nhập đáp án" required />
                                <input type="text" name="Questions[${index}].Options[0].OptionLabel" class="form-control option-label" value="A" readonly />
                                <button type="button" class="btn btn-outline-danger remove-option>
                                    <i class="bi bi-trash"></i>
                                </button>
                            </div>
                            <span class="text-danger field-validation-error" data-valmsg-for="Questions[${index}].Options[0].OptionText" data-valmsg-replace="true"></span>
                        </div>
                        <div class="option-item mb-3" data-option-index="1">
                            <input type="hidden" name="Questions[${index}].Options[1].OptionId" value="" />
                            <input type="hidden" name="Questions[${index}].Options[1].QuestionId" value="" />
                            <input type="hidden" name="Questions[${index}].Options[1].IsCorrect" value="false" class="is-correct-hidden" />
                            <div class="input-group">
                                <div class="input-group-text">
                                    <input type="radio" name="Questions[${index}].CorrectOptionIndex" value="1" class="form-check-input" correct-option required />
                                </div>
                                <input type="text" name="Questions[${index}].Options[1].OptionText" class="form-control" option-text placeholder="Nhập đáp án" required />
                                <input type="text" name="Questions[${index}].Options[1].OptionLabel" class="form-control option-label" value="B" readonly />
                                <button type="button" class="btn btn-outline-danger remove-option">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </div>
                            <span class="text-danger field-validation-error" data-valmsg-for="Questions[${index}].Options[1].OptionText" data-valmsg-replace="true"></span>
                        </div>
                    </div>
                    <button type="button" class="btn btn-outline-primary mt-2 add-option">
                        <i class="bi bi-plus-circle me-1"></i> Thêm đáp án
                    </button>
                `;
                item.querySelector(".card-body").appendChild(optionsContainer);
                optionsContainer.querySelectorAll('.correct-option').forEach(radio => {
                    radio.addEventListener('change", function() {
                        updateCorrectOption(this);
                });
            });
            }
        } else {
    if (optionsContainer) {
        optionsContainer.remove();
    }
}
    }

function updateCorrectOption(radio) {
    const optionItem = radio.closest('.option-item');
    const optionsContainer = optionItem.closest('.options-container');
    const questionIndex = optionItem.closest('.question-item').dataset.questionIndex;
    const optionIndex = optionItem.dataset.optionIndex;

    optionsContainer.querySelectorAll('.option-item').forEach(item => {
        const isCorrectInput = item.querySelector('.is-correct-hidden');
        const currentOptionIndex = item.dataset.optionIndex;
        isCorrectInput.value = (currentOptionIndex === optionIndex) ? "true" : "false";
    });
}

questionsContainer.addEventListener("change", function (e) {
    if (e.target.classList.contains("question-type")) {
        const questionItem = e.target.closest(".question-item");
        const questionIndex = questionItem.dataset.questionIndex;
        updateQuestionUI(questionItem, questionIndex);
    }
});

questionsContainer.addEventListener("click", function (e) {
    if (e.target.classList.contains("add-question") || e.target.closest(".add-question")) {
        const questionsList = questionsContainer.querySelector(".questions-list");
        const questionCount = questionsList.querySelectorAll(".question-item").length;
        const newQuestion = document.createElement("div");
        newQuestion.className = "question-item card shadow-sm border-0 mb-4";
        newQuestion.dataset.questionIndex = questionCount;
        newQuestion.innerHTML = `
                <div class="card-header bg-primary bg-opacity-10">
                    <div class="d-flex justify-content-between align-items-center">
                        <h5 class="mb-0 d-flex align-items-center">
                            <span class="badge bg-primary me-2">${questionCount + 1}</span> Câu hỏi ${questionCount + 1}
                        </h5>
                        <div>
                            <button type="button" class="btn btn-outline btn-danger btn-sm remove-question">
                                <i class="bi bi-trash"></i> Xóa
                            </button>
                        </div>
                    </div>
                </div>
                <div class="card-body">
                    <input type="hidden" name="Questions[${questionCount}].QuestionId" value="" />
                    <input type="hidden" name="Questions[${questionCount}].AssignmentId" value="@Model.AssignmentId" />
                    <input type="hidden" name="Questions[${questionCount}].OrderNumber" value="${questionCount + 1}" />
                    <div class="row mb-3">
                        <div class="col-md-8">
                            <label for class="form-label fw-bold">Loại câu hỏi <span class="text-danger">*</span></label>
                            <select name="Questions[${questionCount}].QuestionType" class="form-select question-type" required>
                                <option value="Essay">Tự luận</option>
                                <option value="MultipleChoice">Trắc nghiệm</option>
                            </select>
                        </div>
                        <div class="col-md-4">
                            <label for class="form-label">Điểm tối đa <span class="text-danger">*</span>
                            <div class="input-group">
                                <input type="number" name="Questions[${questionCount}].MaxScore" class="form-control max-score"
                                       min="0" max="10" value="0" required onchange="validateTotalScore()" />
                            <span class="input-group-text">/ 10</span>
                            </div>
                            <span class="text-danger field-validation-error" data-valmsg-for="Questions[${questionCount}].MaxScore" data-valmsg-replace="true"></span>
                        </div>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Nội dung câu hỏi <span class="text-danger">*></span></label>
                        <textarea name="Questions[${questionCount}].QuestionText" class="form-control question-text" rows="3" required
                                          placeholder="Nhập nội dung câu hỏi tại đây..."></textarea>
                            <span class="text-danger field-validation-error" data-valmsg-for="Questions[${questionCount}].QuestionText" data-valmsg-replace="true"></span>
                    </div>
                </div>
            `;
        questionsList.appendChild(newQuestion);
        updateQuestionUI(newQuestion, questionCount);
        updateTotalScoreDisplay();
    }
});

questionsContainer.addEventListener("click", function (e) {
    if (e.target.classList.contains("add-option") || e.target.closest(".add-option")) {
        const questionItem = e.target.closest(".question-item");
        const questionIndex = parseInt(questionItem.dataset.questionIndex);
        const optionsList = questionItem.querySelector(".options-list");
        const optionCount = optionsList.querySelectorAll(".option-item").length;
        const newOption = document.createElement("div");
        newOption.className = "option-item mb-3";
        newOption.dataset.optionIndex = optionCount;
        newOption.innerHTML = `
                <input type="hidden" name="Questions[${questionIndex}].Options[${optionCount}].OptionId" value="" />
                <input type="hidden" name="Questions[${questionIndex}].Options[${optionCount}].QuestionId" value="" />
                <input type="hidden" name="Questions[${questionIndex}].Options[${optionCount}].IsCorrect" value="false" class="is-correct-hidden" />
                <div class="input-group">
                    <div class="input-group-text">
                        <input type="radio" name="Questions[${questionIndex}].CorrectOptionIndex" value="${optionCount}" class="form-check-input correct-option" required />
                    </div>
                    <input type="text" name="Questions[${questionIndex}].Options[${optionCount}].OptionText" class="form-control option-text" required placeholder="Nhập đáp án" />
                    <input type="text" name="Questions[${questionIndex}].Options[${optionCount}].OptionLabel" class="form-control option-label" value="${String.fromCharCode(65 + optionCount)}" readonly />
                    <button type="button" class="btn btn-outline-danger remove-option">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
                <span class="text-danger field-validation-error" data-valmsg-for="Questions[${questionIndex}].Options[${optionCount}].OptionText" data-valmsg-replace="true"></span>
            `;
        optionsList.appendChild(newOption);
        newOption.querySelector('.correct-option').addEventListener('change', function () {
            updateCorrectOption(this);
        });
        updateOptionIndices(optionsList.closest(".options-container"));
    }
});

questionsContainer.addEventListener("click", function (e) {
    if (e.target.classList.contains("remove-question") || e.target.closest(".remove-question")) {
        const questionItems = questionsContainer.querySelectorAll(".question-item");
        if (questionItems.length > 1) {
            e.target.closest(".question-item").remove();
            updateQuestionIndices();
            updateTotalScoreDisplay();
        } else {
            alert("Phải có ít nhất một câu hỏi!");
            return;
        }
    }
});

questionsContainer.addEventListener("click", function (e) {
    if (e.target.classList.contains("remove-option") || e.target.closest(".remove-option")) {
        const optionsContainer = e.target.closest(".options-container");
        const optionItems = optionsContainer.querySelectorAll(".option-item");
        if (optionItems.length > 2) {
            const removedItem = e.target.closest(".option-item");
            const wasCorrect = removedItem.querySelector('.correct-option').checked;
            removedItem.remove();
            updateOptionIndices(optionsContainer);
            if (wasCorrect) {
                const firstRadio = optionsContainer.querySelector('.correct-option');
                if (firstRadio) {
                    firstRadio.checked = true;
                    updateCorrectOption(firstRadio);
                }
            }
        } else {
            alert("Phải có ít nhất hai đáp án cho câu hỏi trắc nghiệm!");
        }
    }
});

function updateQuestionIndices() {
    const questionItems = questionsContainer.querySelectorAll(".question-item");
    questionItems.forEach((item, index) => {
        item.dataset.questionIndex = index;
        const inputs = item.querySelectorAll("input, textarea, select");
        inputs.forEach(input => {
            if (input.name) {
                input.name = input.name.replace(/Questions\[\d+\]/g, `Questions[${index}]`);
            }
        });
        const questionTitle = item.querySelector("h5");
        const badgeNumber = questionTitle.querySelector(".badge");
        badgeNumber.textContent = index + 1;
        questionTitle.innerHTML = questionTitle.innerHTML.replace(/Câu hỏi \d+/, `Câu hỏi ${index + 1}`);
        const orderNumberInput = item.querySelector("input[name*='OrderNumber']");
        if (orderNumberInput) orderNumberInput.value = index + 1;
        const optionsContainer = item.querySelector(".options-container");
        if (optionsContainer) {
            updateOptionIndices(optionsContainer);
        }
    });
}

function updateOptionIndices(optionsContainer) {
    const optionItems = optionsContainer.querySelectorAll(".option-item");
    const questionIndex = optionsContainer.closest(".question-item").dataset.questionIndex;
    optionItems.forEach((item, index) => {
        item.dataset.optionIndex = index;
        const inputs = item.querySelectorAll("input");
        inputs.forEach(input => {
            if (input.name) {
                input.name = input.name.replace(/Questions\[\d+\]\.Options\[\d+\]/g, `Questions[${questionIndex}].Options[${index}]`);
                if (input.type === "radio") {
                    input.value = index;
                    input.name = `Questions[${questionIndex}].CorrectOptionIndex`;
                }
                if (input.name.includes(".OptionLabel")) {
                    input.value = String.fromCharCode(65 + index);
                }
            }
        });
        const radio = item.querySelector('.correct-option');
        if (radio) {
            radio.addEventListener('change', function () {
                updateCorrectOption(this);
            });
        }
    });
    const radioButtons = optionsContainer.querySelectorAll('.correct-option');
    const hasChecked = Array.from(radioButtons).some(radio => radio.checked);
    if (!hasChecked && radioButtons.length > 0) {
        radioButtons[0].checked = true;
        updateCorrectOption(radioButtons[0]);
    }
}

const questionItems = questionsContainer.querySelectorAll(".question-item");
questionItems.forEach((item, index) => {
    updateQuestionUI(item, index);
    const optionsContainer = item.querySelector('.options-container');
    if (optionsContainer) {
        optionsContainer.querySelectorAll('.correct-option').forEach(radio => {
            radio.addEventListener('change', function () {
                updateCorrectOption(this);
            });
        });
    }
});

updateTotalScoreDisplay();

// Đảm bảo CorrectOptionIndex được gửi đúng khi submit
document.querySelector('form').addEventListener('submit', function (e) {
    document.querySelectorAll('.question-item').forEach(function (questionItem) {
        const questionIndex = questionItem.dataset.questionIndex;
        const checkedRadio = questionItem.querySelector('.correct-option:checked');
        if (checkedRadio) {
            const optionIndex = checkedRadio.value;
            // Đặt lại tất cả IsCorrect về false
            questionItem.querySelectorAll('.is-correct-hidden').forEach(function (input) {
                input.value = "false";
            });
            // Đặt IsCorrect của đáp án đúng về true
            const correctInput = questionItem.querySelector(`input[name="Questions[${questionIndex}].Options[${optionIndex}].IsCorrect"]`);
            if (correctInput) {
                correctInput.value = "true";
            }
        } else if (questionItem.querySelector('.options-container')) {
            // Nếu là câu hỏi trắc nghiệm nhưng không có đáp án nào được chọn
            e.preventDefault();
            alert(`Vui lòng chọn đáp án đúng cho câu hỏi ${parseInt(questionIndex) + 1}!`);
        }
    });
});
});