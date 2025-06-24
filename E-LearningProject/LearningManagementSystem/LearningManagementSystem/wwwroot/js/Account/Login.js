// Password Toggle Functionality
document.querySelectorAll('.toggle-password').forEach(function(toggle) {
    toggle.addEventListener('click', function() {
        var targetId = this.getAttribute('data-target');
        var input = document.getElementById(targetId);
        if (input.type === 'password') {
            input.type = 'text';
            this.innerHTML = '<i class="bi bi-eye-slash"></i>';
        } else {
            input.type = 'password';
            this.innerHTML = '<i class="bi bi-eye"></i>';
        }
    });
});

// Form Submission with Loading Animation
document.getElementById('loginForm').addEventListener('submit', function(e) {
    const submitBtn = this.querySelector('button[type="submit"]');
    submitBtn.classList.add('btn-loading');
    submitBtn.disabled = true;

    // Remove loading state after 3 seconds (adjust as needed)
    setTimeout(() => {
        submitBtn.classList.remove('btn-loading');
        submitBtn.disabled = false;
    }, 3000);
});

// Enhanced floating animation
function createFloatingParticles() {
    const container = document.querySelector('.floating-icons');
    const icons = ['bi-book', 'bi-lightbulb', 'bi-award', 'bi-star', 'bi-trophy', 'bi-gem', 'bi-bookmark-star', 'bi-puzzle'];

    setInterval(() => {
        if (container.children.length < 15) {
            const particle = document.createElement('i');
            particle.className = `bi ${ icons[Math.floor(Math.random() * icons.length)] } floating - icon`;
            particle.style.left = Math.random() * 100 + '%';
            particle.style.top = Math.random() * 100 + '%';
            particle.style.animationDelay = Math.random() * 5 + 's';
            particle.style.fontSize = (Math.random() * 1.5 + 1) + 'rem';
            particle.style.animationDuration = (Math.random() * 4 + 4) + 's';

            container.appendChild(particle);

            setTimeout(() => {
                if (particle.parentNode) {
                    particle.remove();
                }
            }, 10000);
        }
    }, 2000);
}

// Input focus animations
document.querySelectorAll('.form-control').forEach(input => {
    input.addEventListener('focus', function() {
        this.parentElement.classList.add('focused');
    });

    input.addEventListener('blur', function() {
        this.parentElement.classList.remove('focused');
    });
});

// Show error alert (if needed)
function showError(message) {
    const alert = document.getElementById('errorAlert');
    const messageSpan = document.getElementById('errorMessage');
    messageSpan.textContent = message;
    alert.style.display = 'block';

    setTimeout(() => {
        alert.style.display = 'none';
    }, 5000);
}

// Initialize
document.addEventListener('DOMContentLoaded', function() {
    createFloatingParticles();

    // Check for server-side errors
    const tempDataError = '@TempData["Error"]';
    if (tempDataError && tempDataError !== '') {
        showError(tempDataError);
    }
});