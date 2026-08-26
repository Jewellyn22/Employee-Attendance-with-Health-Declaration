// Scanner detection module - prevents manual typing while allowing scanner input
const ScannerDetection = {
    config: {
        maxKeystrokeDelay: 100, // ms - if delay > this, it's manual typing (reduced from 150 for stricter detection)
        minScannerSpeed: 50,    // ms - scanner must be faster than this
        warningMessage: 'Please scan your ID.'
    },

    init: function(fieldId, enabled) {
        if (!enabled) return; // Only activate if config says so

        const field = document.getElementById(fieldId);
        if (!field) return;

        let firstKeyTime = null;
        let lastKeyTime = null;
        let keyCount = 0;
        let isManualTyping = false;

        field.addEventListener('keydown', function(e) {
            // Ignore Enter key - it's for form submission, not scanning detection
            if (e.key === 'Enter' || e.which === 13) {
                console.log('Scanner detection: Enter key detected - ignoring and allowing form submission');
                // Reset detection state so the next scan starts a fresh burst window;
                // its first key must not be timed against the previous scan's last key
                isManualTyping = false;
                firstKeyTime = null;
                lastKeyTime = null;
                keyCount = 0;
                return;
            }

            const now = Date.now();

            if (firstKeyTime === null) {
                firstKeyTime = now;
                lastKeyTime = now;
                keyCount = 1;
                return;
            }

            const timeSinceLastKey = now - lastKeyTime;
            const timeSinceFirstKey = now - firstKeyTime;
            lastKeyTime = now;
            keyCount++;

            // Check if this looks like manual typing (delay between keys)
            if (timeSinceLastKey > ScannerDetection.config.maxKeystrokeDelay) {
                isManualTyping = true;
            }
        });

        field.addEventListener('input', function() {
            console.log('Scanner detection: input event, isManualTyping:', isManualTyping, 'field value:', field.value);

            if (isManualTyping) {
                console.log('Scanner detection: Clearing field due to manual typing');
                // Clear the field and show warning
                field.value = '';
                isManualTyping = false;
                firstKeyTime = null;
                lastKeyTime = null;
                keyCount = 0;

                // Show warning message
                ScannerDetection.showWarning(field);
            }
        });

        // Reset on field blur
        field.addEventListener('blur', function() {
            firstKeyTime = null;
            lastKeyTime = null;
            keyCount = 0;
            isManualTyping = false;
        });
    },

    showWarning: function(field) {
        // Remove existing warning if present
        const existingWarning = field.parentNode.querySelector('.scanner-warning');
        if (existingWarning) existingWarning.remove();

        // Create warning element
        const warning = document.createElement('div');
        warning.className = 'scanner-warning text-danger mt-1';
        warning.style.fontSize = '0.875rem';
        warning.textContent = ScannerDetection.config.warningMessage;

        field.parentNode.appendChild(warning);

        // Auto-remove after 3 seconds
        setTimeout(function() {
            if (warning.parentNode) warning.remove();
        }, 3000);
    }
};
