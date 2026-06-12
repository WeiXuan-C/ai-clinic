// Scroll to bottom of a container
window.scrollToBottom = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Download file helper - supports data URLs and base64
window.downloadFile = function(fileName, base64Data, contentType) {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    // Clean up
    setTimeout(() => URL.revokeObjectURL(url), 100);
};

// Download file from byte array
window.downloadFileFromBytes = function(fileName, contentType, byteArray) {
    const blob = new Blob([new Uint8Array(byteArray)], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
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
