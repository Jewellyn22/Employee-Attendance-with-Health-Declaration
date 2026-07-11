const LoginPage = {
    init: function() {
        this.setupForm();
    },

    setupForm: function() {
        const self = this;
        const loginForm = $('#login-form');
        const loginButton = $('#login-button');
        const usernameInput = $('#username');
        const passwordInput = $('#password');

        loginForm.on('submit', function(e) {
            e.preventDefault();

            // Client-side validation
            const username = usernameInput.val().trim();
            const password = passwordInput.val().trim();

            if (!username) {
                self.showError('Please enter your username');
                usernameInput.focus();
                return;
            }

            if (!password) {
                self.showError('Please enter your password');
                passwordInput.focus();
                return;
            }

            // Get return URL from hidden field
            const returnUrl = $('input[name="returnUrl"]').val() || '/Admin';

            // Show loading state
            self.setLoadingState(true);

            // Prepare form data
            const formData = {
                username: username,
                password: password,
                returnUrl: returnUrl
            };

            // AJAX form submission
            $.ajax({
                url: '/Auth/Login',
                type: 'POST',
                data: formData,
                success: function(response) {
                    self.setLoadingState(false);

                    if (response.success) {
                        // Show success message
                        self.showSuccess(response.message || 'Login successful');

                        // Redirect after short delay
                        setTimeout(function() {
                            window.location.href = response.redirect || returnUrl;
                        }, 1000);
                    } else {
                        // Show error message
                        self.showError(response.message || 'Login failed');
                    }
                },
                error: function(xhr, status, error) {
                    self.setLoadingState(false);

                    // Handle different error scenarios
                    if (xhr.status === 401) {
                        self.showError('Invalid credentials');
                    } else if (xhr.status === 403) {
                        self.showError('Access denied');
                    } else if (xhr.status === 500) {
                        self.showError('Server error. Please try again later.');
                    } else {
                        self.showError('Network error. Please check your connection.');
                    }

                    console.error('Login error:', {
                        status: xhr.status,
                        statusText: xhr.statusText,
                        responseText: xhr.responseText
                    });
                }
            });
        });

        // Handle Enter key in form fields
        usernameInput.on('keypress', function(e) {
            if (e.which === 13) {
                passwordInput.focus();
            }
        });
    },

    setLoadingState: function(isLoading) {
        const loginButton = $('#login-button');
        const usernameInput = $('#username');
        const passwordInput = $('#password');

        if (isLoading) {
            // Disable button and show loading
            loginButton.prop('disabled', true);
            loginButton.html('<i class="fas fa-spinner fa-spin"></i> Logging in...');
            usernameInput.prop('readonly', true);
            passwordInput.prop('readonly', true);
        } else {
            // Enable button and restore text
            loginButton.prop('disabled', false);
            loginButton.html('<i class="fas fa-sign-in-alt"></i> Login');
            usernameInput.prop('readonly', false);
            passwordInput.prop('readonly', false);
        }
    },

    showSuccess: function(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'success',
                title: 'Success',
                text: message,
                timer: 1500,
                showConfirmButton: false,
                toast: true,
                position: 'top-end'
            });
        } else {
            alert('Success: ' + message);
        }
    },

    showError: function(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: 'Login Failed',
                text: message,
                timer: 3000,
                showConfirmButton: false,
                toast: true,
                position: 'top-end'
            });
        } else {
            alert('Error: ' + message);
        }
    }
};

// Initialize login page when document is ready
$(document).ready(function() {
    LoginPage.init();
});

// Global logout function
async function logout() {
    try {
        const response = await fetch('/Auth/Logout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const result = await response.json();

        if (result.success) {
            // Clear all client-side storage
            sessionStorage.clear();
            localStorage.clear();

            // Redirect to home
            window.location.href = result.redirect;
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Logout Failed',
                text: result.message || 'An error occurred during logout'
            });
        }
    } catch (error) {
        console.error('Logout error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Logout Error',
            text: 'An error occurred. Please try again.'
        });
    }
}