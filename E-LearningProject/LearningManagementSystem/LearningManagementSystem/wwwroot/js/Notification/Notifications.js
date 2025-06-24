document.addEventListener('DOMContentLoaded', function () {
    // Auto-dismiss system alerts
    const alerts = document.querySelectorAll('#systemAlerts .alert');
    alerts.forEach(alert => {
        setTimeout(function () {
            const wrapper = alert.closest('.alert-wrapper');
            wrapper.classList.remove('animate__fadeInDown');
            wrapper.classList.add('animate__fadeOutUp');
            setTimeout(() => wrapper.remove(), 500);
        }, 5000);
    });

    // Filtering functionality
    const filterButtons = document.querySelectorAll('.btn-filter');
    const notificationCards = document.querySelectorAll('.notification-card');
    const searchInput = document.getElementById('searchNotifications');
    const emptyDiv = document.querySelector('.notification-empty');

    function updateEmptyState() {
        let visible = 0;
        notificationCards.forEach(card => {
            if (card.style.display !== 'none') visible++;
        });
        if (visible === 0) {
            emptyDiv.style.display = '';
        } else {
            emptyDiv.style.display = 'none';
        }
    }

    filterButtons.forEach(button => {
        button.addEventListener('click', function () {
            // Update active state
            filterButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');

            // Filter notifications
            const filterType = this.dataset.filter;
            notificationCards.forEach(card => {
                card.style.display = 'flex'; // Reset display
                if (filterType === 'unread' && card.classList.contains('read')) {
                    card.style.display = 'none';
                } else if (filterType === 'read' && card.classList.contains('unread')) {
                    card.style.display = 'none';
                }
            });
            // Also apply search filter
            if (searchInput.value) {
                applySearch(searchInput.value);
            }
            updateEmptyState();
        });
    });

    // Search functionality
    function applySearch(query) {
        query = query.toLowerCase();
        let visible = 0;
        notificationCards.forEach(card => {
            if (card.style.display === 'none') return; // Skip already filtered out cards
            const title = card.querySelector('.notification-title').textContent.toLowerCase();
            const content = card.querySelector('.notification-text').textContent.toLowerCase();
            if (title.includes(query) || content.includes(query)) {
                card.style.display = 'flex';
                visible++;
            } else {
                card.style.display = 'none';
            }
        });
        emptyDiv.style.display = visible === 0 ? '' : 'none';
    }

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            applySearch(this.value);
        });
    }

    // Mark all as read button
    const markAllReadBtn = document.querySelector('.mark-all-read');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', function (e) {
            e.preventDefault();
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            markAllReadBtn.innerHTML = '<i class="bi bi-check2-all me-2"></i>Đang xử lý...';
            markAllReadBtn.disabled = true;
            fetch('/Notification/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: `__RequestVerificationToken=${token}`
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    notificationCards.forEach(card => {
                        if (card.classList.contains('unread')) {
                            card.classList.remove('unread');
                            card.classList.add('read');
                            card.querySelector('.status-indicator').classList.remove('unread');
                            const actionBtn = card.querySelector('.btn-mark-read');
                            if (actionBtn) {
                                const parentDiv = actionBtn.closest('.notification-actions');
                                actionBtn.closest('form').remove();
                                const readMarker = document.createElement('div');
                                readMarker.className = 'read-marker';
                                readMarker.innerHTML = '<i class="bi bi-check2-all"></i><span>Đã đọc</span>';
                                parentDiv.appendChild(readMarker);
                            }
                        }
                    });
                    document.querySelectorAll('.stat-value')[1].textContent = '0';
                    document.querySelectorAll('.stat-value')[2].textContent = notificationCards.length;
                    markAllReadBtn.closest('.stat-action').remove();
                    updateEmptyState();
                }
            });
        });
    }

    // Add hover effect to notification cards
    notificationCards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.backgroundColor = this.classList.contains('unread') ? '#e2e9f7' : '#f3f4f6';
        });
        card.addEventListener('mouseleave', function () {
            this.style.backgroundColor = this.classList.contains('unread') ? 'var(--unread)' : 'var(--read)';
        });
    });

    // Initial check for empty state
    updateEmptyState();
});