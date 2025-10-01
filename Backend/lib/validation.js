/**
 * Validation utilities for the Clicker Game Backend
 * Provides reusable validation functions for user input
 */

/**
 * Validates email format
 * @param {string} email - Email to validate
 * @returns {boolean} True if email is valid
 */
function validateEmail(email) {
    if (typeof email !== 'string') return false;
    
    // RFC 5322 compliant email regex (simplified)
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email) && email.length <= 100;
}

/**
 * Validates username format
 * @param {string} username - Username to validate
 * @returns {boolean} True if username is valid
 */
function validateUsername(username) {
    if (typeof username !== 'string') return false;
    
    // Username: 3-30 chars, alphanumeric, underscores, hyphens
    const usernameRegex = /^[a-zA-Z0-9_-]{3,30}$/;
    return usernameRegex.test(username);
}

/**
 * Validates password strength
 * @param {string} password - Password to validate
 * @returns {boolean} True if password meets requirements
 */
function validatePassword(password) {
    if (typeof password !== 'string') return false;
    
    // Password: min 6 chars, max 128, at least one letter and one number
    if (password.length < 6 || password.length > 128) return false;
    
    const passwordRegex = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{6,}$/;
    return passwordRegex.test(password);
}

/**
 * Sanitizes input by trimming whitespace and removing dangerous characters
 * @param {any} input - Input to sanitize
 * @returns {any} Sanitized input
 */
function sanitizeInput(input) {
    if (typeof input !== 'string') return input;
    
    return input
        .trim()
        .replace(/[<>]/g, '') // Remove basic HTML tags
        .replace(/javascript:/gi, '') // Remove javascript: protocol
        .replace(/on\w+=/gi, ''); // Remove event handlers
}

/**
 * Validates that a string is not empty after sanitization
 * @param {string} input - Input to validate
 * @returns {boolean} True if input is valid
 */
function validateNotEmpty(input) {
    if (typeof input !== 'string') return false;
    return sanitizeInput(input).length > 0;
}

/**
 * Validates string length within specified bounds
 * @param {string} input - Input to validate
 * @param {number} min - Minimum length (default: 0)
 * @param {number} max - Maximum length (default: Infinity)
 * @returns {boolean} True if length is valid
 */
function validateLength(input, min = 0, max = Infinity) {
    if (typeof input !== 'string') return false;
    const length = input.length;
    return length >= min && length <= max;
}

/**
 * Validates that input contains only alphanumeric characters
 * @param {string} input - Input to validate
 * @returns {boolean} True if alphanumeric
 */
function validateAlphanumeric(input) {
    if (typeof input !== 'string') return false;
    const alphanumericRegex = /^[a-zA-Z0-9]+$/;
    return alphanumericRegex.test(input);
}

/**
 * Validates numeric input
 * @param {any} input - Input to validate
 * @param {number} min - Minimum value (optional)
 * @param {number} max - Maximum value (optional)
 * @returns {boolean} True if valid number
 */
function validateNumber(input, min = -Infinity, max = Infinity) {
    const num = Number(input);
    return !isNaN(num) && isFinite(num) && num >= min && num <= max;
}

/**
 * Validates integer input
 * @param {any} input - Input to validate
 * @param {number} min - Minimum value (optional)
 * @param {number} max - Maximum value (optional)
 * @returns {boolean} True if valid integer
 */
function validateInteger(input, min = -Infinity, max = Infinity) {
    const num = Number(input);
    return Number.isInteger(num) && num >= min && num <= max;
}

/**
 * Comprehensive user registration validation
 * @param {Object} userData - User data to validate
 * @param {string} userData.username - Username
 * @param {string} userData.email - Email
 * @param {string} userData.password - Password
 * @returns {Object} Validation result with success and errors
 */
function validateUserRegistration({ username, email, password }) {
    const errors = [];

    // Check required fields
    if (!username || !email || !password) {
        errors.push('All fields (username, email, password) are required');
        return { success: false, errors };
    }

    // Validate types
    if (typeof username !== 'string' || typeof email !== 'string' || typeof password !== 'string') {
        errors.push('All fields must be strings');
        return { success: false, errors };
    }

    // Sanitize and validate username
    const sanitizedUsername = sanitizeInput(username);
    if (!validateNotEmpty(sanitizedUsername)) {
        errors.push('Username cannot be empty');
    } else if (!validateUsername(sanitizedUsername)) {
        errors.push('Username must be 3-30 characters long and contain only letters, numbers, underscores, and hyphens');
    }

    // Sanitize and validate email
    const sanitizedEmail = sanitizeInput(email);
    if (!validateNotEmpty(sanitizedEmail)) {
        errors.push('Email cannot be empty');
    } else if (!validateEmail(sanitizedEmail)) {
        errors.push('Please provide a valid email address (max 100 characters)');
    }

    // Sanitize and validate password
    const sanitizedPassword = sanitizeInput(password);
    if (!validateNotEmpty(sanitizedPassword)) {
        errors.push('Password cannot be empty');
    } else if (!validatePassword(sanitizedPassword)) {
        errors.push('Password must be 6-128 characters long and contain at least one letter and one number');
    }

    return {
        success: errors.length === 0,
        errors,
        sanitizedData: errors.length === 0 ? {
            username: sanitizedUsername,
            email: sanitizedEmail,
            password: sanitizedPassword
        } : null
    };
}

/**
 * Comprehensive user login validation
 * @param {Object} loginData - Login data to validate
 * @param {string} loginData.username - Username or email
 * @param {string} loginData.password - Password
 * @returns {Object} Validation result with success and errors
 */
function validateUserLogin({ username, password }) {
    const errors = [];

    // Check required fields
    if (!username || !password) {
        errors.push('Username and password are required');
        return { success: false, errors };
    }

    // Validate types
    if (typeof username !== 'string' || typeof password !== 'string') {
        errors.push('Username and password must be strings');
        return { success: false, errors };
    }

    // Sanitize and validate username
    const sanitizedUsername = sanitizeInput(username);
    if (!validateNotEmpty(sanitizedUsername)) {
        errors.push('Username cannot be empty');
    } else if (!validateLength(sanitizedUsername, 1, 100)) {
        errors.push('Username is too long');
    }

    // Sanitize and validate password
    const sanitizedPassword = sanitizeInput(password);
    if (!validateNotEmpty(sanitizedPassword)) {
        errors.push('Password cannot be empty');
    } else if (!validateLength(sanitizedPassword, 1, 128)) {
        errors.push('Password is too long');
    }

    return {
        success: errors.length === 0,
        errors,
        sanitizedData: errors.length === 0 ? {
            username: sanitizedUsername,
            password: sanitizedPassword
        } : null
    };
}

/**
 * Validates password change data
 * @param {Object} passwordData - Password change data
 * @param {string} passwordData.currentPassword - Current password
 * @param {string} passwordData.newPassword - New password
 * @returns {Object} Validation result
 */
function validatePasswordChange({ currentPassword, newPassword }) {
    const errors = [];

    // Check required fields
    if (!currentPassword || !newPassword) {
        errors.push('Current password and new password are required');
        return { success: false, errors };
    }

    // Validate types
    if (typeof currentPassword !== 'string' || typeof newPassword !== 'string') {
        errors.push('Passwords must be strings');
        return { success: false, errors };
    }

    // Sanitize and validate current password
    const sanitizedCurrentPassword = sanitizeInput(currentPassword);
    if (!validateNotEmpty(sanitizedCurrentPassword)) {
        errors.push('Current password cannot be empty');
    } else if (!validateLength(sanitizedCurrentPassword, 1, 128)) {
        errors.push('Current password is too long');
    }

    // Sanitize and validate new password
    const sanitizedNewPassword = sanitizeInput(newPassword);
    if (!validateNotEmpty(sanitizedNewPassword)) {
        errors.push('New password cannot be empty');
    } else if (!validatePassword(sanitizedNewPassword)) {
        errors.push('New password must be 6-128 characters long and contain at least one letter and one number');
    }

    return {
        success: errors.length === 0,
        errors,
        sanitizedData: errors.length === 0 ? {
            currentPassword: sanitizedCurrentPassword,
            newPassword: sanitizedNewPassword
        } : null
    };
}

module.exports = {
    // Basic validation functions
    validateEmail,
    validateUsername,
    validatePassword,
    sanitizeInput,
    validateNotEmpty,
    validateLength,
    validateAlphanumeric,
    validateNumber,
    validateInteger,
    
    // Comprehensive validation functions
    validateUserRegistration,
    validateUserLogin,
    validatePasswordChange
};