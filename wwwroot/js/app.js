// Scroll to bottom of a container
window.scrollToBottom = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Download file helper
window.downloadFile = function(fileName, base64Data) {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = 'data:application/octet-stream;base64,' + base64Data;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Initialize Lucide icons
window.initializeLucideIcons = function() {
    if (typeof lucide !== 'undefined' && lucide.createIcons) {
        lucide.createIcons();
    }
};

// Auto-initialize Lucide icons on page load
document.addEventListener('DOMContentLoaded', function() {
    window.initializeLucideIcons();
});
