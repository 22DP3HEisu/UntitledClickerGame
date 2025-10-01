var bcrypt = require('bcrypt');
var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { validatePasswordChange } = require('../lib/validation');

// Get current user profile (protected route)
router.get('/', authenticateToken, async function(req, res, next) {
    try {
        // req.user contains the decoded token info from authenticateToken middleware
        const query = 'SELECT UserID, Username, Email, CreatedAt FROM Users WHERE UserID = ?';
        const users = await executeQuery(query, [req.user.id]);

        if (users.length === 0) {
            return res.status(404).json({ 
                error: 'User not found',
                message: 'User profile not found'
            });
        }

        const user = users[0];
        res.json({
            message: 'Profile retrieved successfully',
            user: {
                id: user.UserID,
                username: user.Username,
                email: user.Email,
                createdAt: user.CreatedAt
            }
        });

    } catch (error) {
        console.error('Profile error:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Change password (protected route)
router.put('/change-password', authenticateToken, async function(req, res, next) {
    try {
        const { currentPassword, newPassword } = req.body;

        // Validate input using centralized validation
        const validation = validatePasswordChange({ currentPassword, newPassword });
        if (!validation.success) {
            return res.status(400).json({
                error: 'Invalid input',
                message: validation.errors[0] // Return first error
            });
        }

        const { currentPassword: sanitizedCurrentPassword, newPassword: sanitizedNewPassword } = validation.sanitizedData;

        // Get current user with password hash
        const getUserQuery = 'SELECT UserID, PasswordHash FROM Users WHERE UserID = ?';
        const users = await executeQuery(getUserQuery, [req.user.id]);

        if (users.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User account not found'
            });
        }

        const user = users[0];

        // Verify current password
        const isCurrentPasswordValid = await bcrypt.compare(sanitizedCurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid) {
            return res.status(401).json({
                error: 'Invalid current password',
                message: 'Current password is incorrect'
            });
        }

        // Check if new password is different from current password
        const isSamePassword = await bcrypt.compare(sanitizedNewPassword, user.PasswordHash);
        if (isSamePassword) {
            return res.status(400).json({
                error: 'Invalid new password',
                message: 'New password must be different from current password'
            });
        }

        // Hash new password
        const saltRounds = 12; // Consistent with registration
        const newPasswordHash = await bcrypt.hash(sanitizedNewPassword, saltRounds);

        // Update password in database
        const updateQuery = 'UPDATE Users SET PasswordHash = ?, UpdatedAt = CURRENT_TIMESTAMP WHERE UserID = ?';
        await executeQuery(updateQuery, [newPasswordHash, req.user.id]);

        // Return success response (don't include sensitive data)
        res.json({
            message: 'Password changed successfully'
        });

    } catch (error) {
        console.error('Change password error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to change password'
        });
    }
});

module.exports = router;
