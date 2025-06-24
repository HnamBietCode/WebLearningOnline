document.addEventListener("DOMContentLoaded", function () {
    const questionsContainer = document.getElementById("questionsContainer");
    const scoreProgressBar = document.getElementById("scoreProgressBar");
    const addQuestionButton = document.querySelector(".add-question");

    // Hàm kiểm tra tổng điểm tối đa và cập nhật thanh tiến trình
    window.validateAndUpdateProgress = function () {
        const maxScoreInputs = document.querySelectorAll(".max-score");
        let totalScore = 0;
        maxScoreInputs.forEach(input => {
            const score = parseFloat(input.value) || 0;
            totalScore += score;
        });

        const percentage = Math.min(totalScore * 10, 100);
        scoreProgressBar.style.width = percentage + "%";
        scoreProgressBar.setAttribute("aria-valuenow", totalScore);
        scoreProgressBar.textContent = totalScore + "/10 điểm";

        if (totalScore === 10) {
            scoreProgressBar.classList.remove("bg-danger", "bg-warning");
            scoreProgressBar.classList.add("bg-success");
        } else if (totalScore > 10) {
            scoreProgressBar.classList.remove("bg-success", "bg-warning");
            scoreProgressBar.classList.add("bg-danger");
        } else {
            scoreProgressBar.classList.remove("bg-success", "bg-danger");
            scoreProgressBar.classList.add("bg-warning");
        }

        return totalScore;
    };

    window.validateTotalScore = function () {
        const maxScoreInputs = document.querySelectorAll(".max-score");
        let hasZeroScore = false;

        maxScoreInputs.forEach(input => {
            const score = parseFloat(input.value) || 0;
            if (score <= 0) {
                hasZeroScore = true;
            }
        });

        if (hasZeroScore) {
            Swal.fire({
                icon: 'error',
                title: 'Lỗi điểm số',
                text: 'Điểm tối đa của mỗi câu hỏi phải lớn hơn 0!',
                confirmButtonText: 'Đã hiểu'
            });
            return false;
        }

        const totalScore = validateAndUpdateProgress();
        if (totalScore !== 10) {
            Swal.fire({
                icon: 'error',
                title: 'Lỗi điểm số',
                text: 'Tổng điểm tối đa của tất cả câu hỏi phải bằng 10!',
                confirmButtonText: 'Đã hiểu'
            });
            return false;
        }
        return true;
    };

    function updateQuestionUI(item, index) {
        const questionTypeSelect = item.querySelector(".question-type");
        const isMultipleChoice = questionTypeSelect.value === "MultipleChoice";
        let optionsContainer = item.querySelector(".options-container");

        if (isMultipleChoice) {
            if (!optionsContainer) {
                optionsContainer = document.createElement("div");
                optionsContainer.className = "options-container mt-4";
                const optionsHeader = document.createElement("div");
                optionsHeader.className = "mb-3";
                optionsHeader.innerHTML = `
                    <label class="form-label fw-bold">Các lựa chọn <span class="text-danger">*</span></label>
                    <div class="form-text mb-2">Chọn đáp án đúng bằng cách nhấp vào nút radio bên trái mỗi đáp án</div>
                `;
                optionsContainer.appendChild(optionsHeader);

                const optionsContent = document.createElement("div");
                optionsContent.className = "options-list";
                optionsContent.innerHTML = `
                    <div class="option-item" data-option-index="0">
                        <input type="hidden" name="Questions[${index}].Options[0].OptionId" value="" />
                        <input type="radio" name="Questions[${index}].CorrectOptionIndex" value="0" class="form-check-input me-2 correct-option" required checked />
                        <input type="text" name="Questions[${index}].Options[0].OptionText" class="form-control option-text" placeholder="Nhập đáp án" required />
                        <span class="text-danger field-validation-error" data-valmsg-for="Questions[${index}].Options[0].OptionText" data-valmsg-replace="true"></span>
                        <input type="text" name="Questions[${index}].Options[0].OptionLabel" class="form-control option-label" value="A" readonly />
                        <button type="button" class="btn btn-outline-danger btn-sm remove-option">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                    <div class="option-item" data-option-index="1">
                        <input type="hidden" name="Questions[${index}].Options[1].OptionId" value="" />
                        <input type="radio" name="Questions[${index}].CorrectOptionIndex" value="1" class="form-check-input me-2 correct-option" required />
                        <input type="text" name="Questions[${index}].Options[1].OptionText" class="form-control option-text" placeholder="Nhập đáp án" required />
                        <span class="text-danger field-validation-error" data-valmsg-for="Questions[${index}].Options[1].OptionText" data-valmsg-replace="true"></span>
                        <input type="text" name="Questions[${index}].Options[1].OptionLabel" class="form-control option-label" value="B" readonly />
                        <button type="button" class="btn btn-outline-danger btn-sm remove-option">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                `;
                optionsContainer.appendChild(optionsContent);

                const addOptionBtn = document.createElement("button");
                addOptionBtn.type = "button";
                addOptionBtn.className = "btn btn-outline-primary btn-sm add-option";
                addOptionBtn.innerHTML = '<i class="bi bi-plus-circle me-1"></i> Thêm đáp án';
                optionsContainer.appendChild(addOptionBtn);

                item.querySelector(".card-body").appendChild(optionsContainer);

                // Thêm sự kiện cho radio button
                optionsContainer.querySelectorAll('.correct-option').forEach(radio => {
                    radio.addEventListener('change', function () {
                        const optionsList = this.closest('.options-list');
                        optionsList.querySelectorAll('.correct-option').forEach(r => r.checked = (r === this));
                    });
                });
            }
        } else {
            if (optionsContainer) {
                optionsContainer.remove();
            }
        }
    }

    questionsContainer.addEventListener("change", function (e) {
        if (e.target.classList.contains("question-type")) {
            const questionItem = e.target.closest(".question-item");
            const questionIndex = questionItem.dataset.questionIndex;
            updateQuestionUI(questionItem, questionIndex);
        }
    });

    addQuestionButton.addEventListener("click", function () {
        const questionCount = questionsContainer.querySelectorAll(".question-item").length;
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
                        <button type="button" class="btn btn-outline-danger btn-sm remove-question">
                            <i class="bi bi-trash"></i> Xóa
                        </button>
                    </div>
                </div>
            </div>
            <div class="card-body">
                <input type="hidden" name="Questions[${questionCount}].OrderNumber" value="${questionCount + 1}" />

                <div class="row mb-3">
                    <div class="col-md-8">
                        <label class="form-label fw-bold">Loại câu hỏi <span class="text-danger">*</span></label>
                        <select name="Questions[${questionCount}].QuestionType" class="form-select question-type" required>
                            <option value="Essay">Tự luận</option>
                            <option value="MultipleChoice">Trắc nghiệm</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label fw-bold">Điểm tối đa <span class="text-danger">*</span></label>
                        <div class="input-group">
                            <input type="number" name="Questions[${questionCount}].MaxScore" class="form-control max-score"
                                   min="1" max="10" value="0" required onchange="validateAndUpdateProgress()" />
                            <span class="input-group-text">/ 10</span>
                        </div>
                        <span class="text-danger field-validation-error" data-valmsg-for="Questions[${questionCount}].MaxScore" data-valmsg-replace="true"></span>
                    </div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-bold">Nội dung câu hỏi <span class="text-danger">*</span></label>
                    <textarea name="Questions[${questionCount}].QuestionText" class="form-control question-text" rows="3" required
                              placeholder="Nhập nội dung câu hỏi tại đây..."></textarea>
                    <span class="text-danger field-validation-error" data-valmsg-for="Questions[${questionCount}].QuestionText" data-valmsg-replace="true"></span>
                </div>
            </div>
        `;
        questionsContainer.appendChild(newQuestion);
        updateQuestionUI(newQuestion, questionCount);
        newQuestion.style.backgroundColor = "#e6fff2";
        setTimeout(() => {
            newQuestion.style.backgroundColor = "";
        }, 500);
        newQuestion.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });

    questionsContainer.addEventListener("click", function (e) {
        if (e.target.classList.contains("add-option") || e.target.closest(".add-option")) {
            const questionItem = e.target.closest(".question-item");
            const questionIndex = questionItem.dataset.questionIndex;
            const optionsContainer = questionItem.querySelector(".options-list");
            const optionCount = optionsContainer.querySelectorAll(".option-item").length;
            const newOption = document.createElement("div");
            newOption.className = "option-item";
            newOption.dataset.optionIndex = optionCount;
            newOption.innerHTML = `
                <input type="hidden" name="Questions[${questionIndex}].Options[${optionCount}].OptionId" value="" />
                <input type="radio" name="Questions[${questionIndex}].CorrectOptionIndex" value="${optionCount}" class="form-check-input me-2 correct-option" required />
                <input type="text" name="Questions[${questionIndex}].Options[${optionCount}].OptionText" class="form-control option-text" placeholder="Nhập đáp án" required />
                <span class="text-danger field-validation-error" data-valmsg-for="Questions[${questionIndex}].Options[${optionCount}].OptionText" data-valmsg-replace="true"></span>
                <input type="text" name="Questions[${questionIndex}].Options[${optionCount}].OptionLabel" class="form-control option-label" value="${String.fromCharCode(65 + optionCount)}" readonly />
                <button type="button" class="btn btn-outline-danger btn-sm remove-option">
                    <i class="bi bi-trash"></i>
                </button>
            `;
            optionsContainer.appendChild(newOption);
            updateOptionIndices(optionsContainer);
            newOption.style.backgroundColor = "#e6fff2";
            setTimeout(() => {
                newOption.style.backgroundColor = "";
            }, 500);
        }
    });

    questionsContainer.addEventListener("click", function (e) {
        if (e.target.classList.contains("remove-question") || e.target.closest(".remove-question")) {
            const questionItems = questionsContainer.querySelectorAll(".question-item");
            if (questionItems.length > 1) {
                const questionItem = e.target.closest(".question-item");
                questionItem.style.opacity = "0.5";
                questionItem.style.transform = "scale(0.98)";
                setTimeout(() => {
                    questionItem.remove();
                    updateQuestionIndices();
                    validateAndUpdateProgress();
                }, 300);
            } else {
                Swal.fire({
                    icon: 'warning',
                    title: 'Không thể xóa',
                    text: 'Phải có ít nhất một câu hỏi!',
                    confirmButtonText: 'Đã hiểu'
                });
            }
        }
    });

    questionsContainer.addEventListener("click", function (e) {
        if (e.target.classList.contains("remove-option") || e.target.closest(".remove-option")) {
            const optionsContainer = e.target.closest(".options-list");
            const optionItems = optionsContainer.querySelectorAll(".option-item");
            if (optionItems.length > 2) {
                const optionItem = e.target.closest(".option-item");
                optionItem.style.opacity = "0.5";
                optionItem.style.transform = "scale(0.98)";
                setTimeout(() => {
                    optionItem.remove();
                    updateOptionIndices(optionsContainer);
                }, 300);
            } else {
                Swal.fire({
                    icon: 'warning',
                    title: 'Không thể xóa',
                    text: 'Phải có ít nhất hai đáp án cho câu hỏi trắc nghiệm!',
                    confirmButtonText: 'Đã hiểu'
                });
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
                    input.name = input.name.replace(/Questions\[\d+\]/, `Questions[${index}]`);
                }
            });

            const questionTitle = item.querySelector("h5");
            const badgeNumber = questionTitle.querySelector(".badge");
            badgeNumber.textContent = index + 1;
            questionTitle.innerHTML = questionTitle.innerHTML.replace(/Câu hỏi \d+/, `Câu hỏi ${index + 1}`);

            const orderNumberInput = item.querySelector("input[name*='OrderNumber']");
            orderNumberInput.value = index + 1;

            const optionsContainer = item.querySelector(".options-container");
            if (optionsContainer) {
                const optionsList = optionsContainer.querySelector(".options-list");
                if (optionsList) {
                    updateOptionIndices(optionsList);
                }
            }
        });
    }

    function updateOptionIndices(optionsContainer) {
        const optionItems = optionsContainer.querySelectorAll(".option-item");
        const questionItem = optionsContainer.closest(".question-item");
        const questionIndex = questionItem.dataset.questionIndex;
        optionItems.forEach((item, index) => {
            item.dataset.optionIndex = index;
            const inputs = item.querySelectorAll("input");
            inputs.forEach(input => {
                if (input.name) {
                    input.name = input.name.replace(/Options\[\d+\]/, `Options[${index}]`);
                }
                if (input.type === "radio") {
                    input.name = `Questions[${questionIndex}].CorrectOptionIndex`;
                    input.value = index;
                    if (index === 0 && !optionsContainer.querySelector(`input[name="Questions[${questionIndex}].CorrectOptionIndex"]:checked`)) {
                        input.checked = true;
                    }
                }
                if (input.name.includes(".OptionLabel")) {
                    input.value = String.fromCharCode(65 + index);
                }
            });
        });
    }

    const questionItems = questionsContainer.querySelectorAll(".question-item");
    questionItems.forEach((item, index) => {
        updateQuestionUI(item, index);
    });

    validateAndUpdateProgress();

    if (typeof Swal === 'undefined') {
        const sweetalertScript = document.createElement('script');
        sweetalertScript.src = 'https://cdn.jsdelivr.net/npm/sweetalert2@11';
        document.head.appendChild(sweetalertScript);
    }

    // Đảm bảo chỉ có một đáp án đúng khi submit
    document.querySelector('form').addEventListener('submit', function (e) {
        document.querySelectorAll('.question-item').forEach(function (questionItem) {
            const questionIndex = questionItem.dataset.questionIndex;
            const optionsContainer = questionItem.querySelector('.options-container');
            if (optionsContainer) {
                const checkedRadio = optionsContainer.querySelector('.correct-option:checked');
                if (!checkedRadio) {
                    e.preventDefault();
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi',
                        text: `Vui lòng chọn đáp án đúng cho câu hỏi ${parseInt(questionIndex) + 1}!`,
                        confirmButtonText: 'Đã hiểu'
                    });
                }
            }
        });
    });
});