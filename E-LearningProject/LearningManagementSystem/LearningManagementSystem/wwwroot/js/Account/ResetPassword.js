
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

// Password Toggle Functionality
document.querySelectorAll('.toggle-password').forEach(toggle => {
    toggle.addEventListener('click', function() {
        const targetId = this.getAttribute('data-target');
        const passwordInput = document.getElementById(targetId);
        const icon = this.querySelector('i');

        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            icon.classList.remove('bi-eye');
            icon.classList.add('bi-eye-slash');
        } else {
            passwordInput.type = 'password';
            icon.classList.remove('bi-eye-slash');
            icon.classList.add('bi-eye');
        }
    });
});

// Input focus animations
document.querySelectorAll('.form-control').forEach(input => {
    input.addEventListener('focus', function() {
        this.parentElement.classList.add('focused');
    });

    input.addEventListener('blur', function() {
        this.parentElement.classList.remove('focused');
    });
});

// Initialize
document.addEventListener('DOMContentLoaded', function() {
    createFloatingParticles();
});
