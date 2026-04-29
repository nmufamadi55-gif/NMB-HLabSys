// Mobile sidebar toggle
document.addEventListener('DOMContentLoaded', function () {
    const menuToggle = document.getElementById('menuToggleBtn');
    const sidebar = document.querySelector('.sidebar-lab');

    if (menuToggle && sidebar) {
        menuToggle.addEventListener('click', function () {
            sidebar.classList.toggle('active');
        });
    }

    // Logout functionality
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', function () {
            window.location.href = '/Home/Index';
        });
    }
});

// Helper function for toast/demo alerts
function showDemoAlert(message) {
    alert('🔔 ' + message + ' (Demo: Backend integration coming in Semester 2)');
}