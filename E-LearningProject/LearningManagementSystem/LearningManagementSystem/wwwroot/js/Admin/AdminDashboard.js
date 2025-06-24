document.addEventListener('DOMContentLoaded', function () {
    // Lấy dữ liệu từ input ẩn
    const rawData = document.getElementById('monthlyPaymentsData').value;
    let monthlyPaymentsData = {};
    try {
        monthlyPaymentsData = JSON.parse(rawData);
    } catch (e) {
        console.error('Lỗi parse dữ liệu MonthlyPayments:', e);
        monthlyPaymentsData = {};
    }

    // Các tháng theo thứ tự từ 1-12
    const monthLabels = ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6',
        'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'];

    // Chuyển đổi dữ liệu từ Dictionary thành mảng theo thứ tự tháng
    const currentYearData = [];
    for (let i = 1; i <= 12; i++) {
        const monthKey = `Tháng ${i}`;
        currentYearData.push(monthlyPaymentsData[monthKey] || 0);
    }

    const currentYear = new Date().getFullYear();
    const paymentData = {
        [currentYear]: currentYearData,
    };

    // Chart.js code giữ nguyên
    const ctx = document.getElementById('paymentChart').getContext('2d');
    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(value);
    }
    const paymentChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: monthLabels,
            datasets: [{
                label: 'Tổng tiền đóng học phí',
                data: currentYearData,
                backgroundColor: 'rgba(67, 97, 238, 0.7)',
                borderColor: 'rgba(67, 97, 238, 1)',
                borderWidth: 1,
                borderRadius: 5,
                maxBarThickness: 50
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        font: {
                            size: 14
                        }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.7)',
                    padding: 12,
                    titleFont: {
                        size: 14
                    },
                    bodyFont: {
                        size: 14
                    },
                    callbacks: {
                        label: function (context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            if (context.parsed.y !== null) {
                                label += formatCurrency(context.parsed.y);
                            }
                            return label;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Tổng tiền (VND)',
                        font: {
                            size: 14,
                            weight: 'bold'
                        }
                    },
                    ticks: {
                        callback: function (value) {
                            if (value >= 1000000) {
                                return (value / 1000000).toLocaleString('vi-VN') + ' triệu';
                            }
                            return value.toLocaleString('vi-VN');
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Tháng',
                        font: {
                            size: 14,
                            weight: 'bold'
                        }
                    }
                }
            }
        }
    });

    // Dropdown năm (nếu có)
    const yearSelector = document.getElementById('yearSelector');
    yearSelector.innerHTML = '';
    const option = document.createElement('option');
    option.value = currentYear;
    option.textContent = currentYear;
    option.selected = true;
    yearSelector.appendChild(option);

    yearSelector.addEventListener('change', function () {
        const selectedYear = this.value;
        if (paymentData[selectedYear]) {
            paymentChart.data.datasets[0].data = paymentData[selectedYear];
            paymentChart.update();
        }
    });

    document.getElementById('exportChart').addEventListener('click', function () {
        const canvas = document.getElementById('paymentChart');
        const image = canvas.toDataURL('image/png');
        const downloadLink = document.createElement('a');
        downloadLink.href = image;
        downloadLink.download = `thong-ke-thanh-toan-${currentYear}.png`;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
    });
});