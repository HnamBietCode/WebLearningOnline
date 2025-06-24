document.addEventListener('DOMContentLoaded', function () {
    // Xử lý chọn tất cả khóa học
    const selectAllCheckbox = document.getElementById('selectAllCourses');
    const courseCheckboxes = document.querySelectorAll('.course-checkbox');
    const checkoutButton = document.getElementById('checkoutButton');
    const checkoutForm = document.getElementById('checkoutForm');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            const isChecked = this.checked;

            courseCheckboxes.forEach(checkbox => {
                checkbox.checked = isChecked;
            });

            updateTotalPrice();
            updateSelectedCount();
            updateSelectedCartItemIds();
            toggleCheckoutButton();
        });
    }

    // Xử lý chọn từng khóa học
    courseCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            updateTotalPrice();
            updateSelectedCount();
            updateSelectedCartItemIds();

            // Kiểm tra xem có nên bỏ tích "Chọn tất cả" không
            if (!this.checked && selectAllCheckbox && selectAllCheckbox.checked) {
                selectAllCheckbox.checked = false;
            }

            // Kiểm tra xem có nên tích "Chọn tất cả" không
            if (selectAllCheckbox) {
                const allChecked = [...courseCheckboxes].every(cb => cb.checked);
                selectAllCheckbox.checked = allChecked;
            }

            toggleCheckoutButton();
        });
    });

    // Cập nhật tổng giá tiền
    function updateTotalPrice() {
        let totalPrice = 0;

        courseCheckboxes.forEach(checkbox => {
            if (checkbox.checked) {
                const price = parseFloat(checkbox.dataset.price || 0);
                totalPrice += price;
            }
        });

        // Cập nhật hiển thị giá
        const totalPriceElement = document.querySelector('.total-price');
        if (totalPriceElement) {
            totalPriceElement.textContent = '₫' + totalPrice.toLocaleString('vi-VN');
        }

        return totalPrice;
    }

    // Cập nhật số lượng khóa học đã chọn
    function updateSelectedCount() {
        const selectedCount = [...courseCheckboxes].filter(cb => cb.checked).length;
        const countElement = document.querySelector('.selected-courses-count');

        if (countElement) {
            countElement.textContent = selectedCount;
        }
    }

    // Cập nhật danh sách các CartItemIds đã chọn
    function updateSelectedCartItemIds() {
        const selectedIds = [];
        courseCheckboxes.forEach(function (checkbox) {
            if (checkbox.checked) {
                selectedIds.push(checkbox.getAttribute('data-id'));
            }
        });

        const selectedCartItemIdsInput = document.getElementById('selectedCartItemIds');
        if (selectedCartItemIdsInput) {
            selectedCartItemIdsInput.value = selectedIds.join(',');
        }

        console.log('Selected IDs:', selectedIds); // Debug log
    }

    // Kích hoạt/tắt nút thanh toán
    function toggleCheckoutButton() {
        if (checkoutButton) {
            const selectedCount = [...courseCheckboxes].filter(cb => cb.checked).length;
            const totalPrice = updateTotalPrice();

            if (selectedCount === 0 || totalPrice <= 0) {
                checkoutButton.disabled = true;
                checkoutButton.classList.add('disabled');
            } else {
                checkoutButton.disabled = false;
                checkoutButton.classList.remove('disabled');
            }
        }
    }

    // Xử lý submit form
    if (checkoutForm) {
        checkoutForm.addEventListener('submit', function (e) {
            const selectedIds = [];
            courseCheckboxes.forEach(function (checkbox) {
                if (checkbox.checked) {
                    selectedIds.push(checkbox.getAttribute('data-id'));
                }
            });

            if (selectedIds.length === 0) {
                e.preventDefault();
                alert('Vui lòng chọn ít nhất một khóa học để thanh toán.');
                return false;
            }

            // Đảm bảo input hidden được cập nhật trước khi submit
            const selectedCartItemIdsInput = document.getElementById('selectedCartItemIds');
            if (selectedCartItemIdsInput) {
                selectedCartItemIdsInput.value = selectedIds.join(',');
            }

            console.log('Form submitting with IDs:', selectedIds); // Debug log
        });
    }

    // Khởi tạo lần đầu
    updateTotalPrice();
    updateSelectedCount();
    updateSelectedCartItemIds();
    toggleCheckoutButton();
});