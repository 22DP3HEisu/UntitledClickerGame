var bcrypt = require('bcrypt');
var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { validatePasswordChange } = require('../lib/validation');

// Get current user profile with game data (protected route)
router.get('/', authenticateToken, async function(req, res, next) {
    try {
        // Get user basic info and game data
        const userQuery = `
            SELECT u.UserID, u.Username, u.Email, u.Carrots, u.HorseShoes, u.G_Carrots, u.CreatedAt,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const users = await executeQuery(userQuery, [req.user.id]);

        if (users.length === 0) {
            return res.status(404).json({ 
                error: 'User not found',
                message: 'User profile not found'
            });
        }

        const user = users[0];

        // If no upgrades record exists, create one with default values
        if (user.Upgrade1 === null) {
            const createUpgradesQuery = 'INSERT INTO User_upgrades (UserID) VALUES (?)';
            await executeQuery(createUpgradesQuery, [req.user.id]);
            
            // Set default upgrade values
            user.Upgrade1 = false;
            user.Upgrade2 = false;
            user.Upgrade3 = false;
        }

        res.json({
            message: 'Profile retrieved successfully',
            user: {
                id: user.UserID,
                username: user.Username,
                email: user.Email,
                createdAt: user.CreatedAt,
                gameData: {
                    carrots: user.Carrots,
                    horseShoes: user.HorseShoes,
                    goldenCarrots: user.G_Carrots,
                    upgrades: {
                        upgrade1: Boolean(user.Upgrade1),
                        upgrade2: Boolean(user.Upgrade2),
                        upgrade3: Boolean(user.Upgrade3)
                    }
                }
            }
        });

    } catch (error) {
        console.error('Profile error:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Sync currency data with server (protected route)
router.put('/sync-currency', authenticateToken, async function(req, res, next) {
    try {
        const { carrots, horseShoes, goldenCarrots } = req.body;

        // Validate currency data
        if (typeof carrots !== 'number' || typeof horseShoes !== 'number' || typeof goldenCarrots !== 'number') {
            return res.status(400).json({
                error: 'Invalid currency data',
                message: 'Currency values must be numbers'
            });
        }

        // Ensure non-negative values (basic anti-cheat)
        if (carrots < 0 || horseShoes < 0 || goldenCarrots < 0) {
            return res.status(400).json({
                error: 'Invalid currency values',
                message: 'Currency values cannot be negative'
            });
        }

        // Get current server values for comparison/logging
        const getCurrentQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
        const currentData = await executeQuery(getCurrentQuery, [req.user.id]);

        if (currentData.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User account not found'
            });
        }

        // Update currency in database
        const updateQuery = `
            UPDATE Users 
            SET Carrots = ?, HorseShoes = ?, G_Carrots = ?, UpdatedAt = CURRENT_TIMESTAMP 
            WHERE UserID = ?
        `;
        await executeQuery(updateQuery, [carrots, horseShoes, goldenCarrots, req.user.id]);

        // Log the sync for debugging (optional)
        console.log(`Currency sync for user ${req.user.id}: 
            Carrots: ${currentData[0].Carrots} -> ${carrots}
            HorseShoes: ${currentData[0].HorseShoes} -> ${horseShoes}
            Golden Carrots: ${currentData[0].G_Carrots} -> ${goldenCarrots}`);

        // Return updated currency data
        res.json({
            message: 'Currency synchronized successfully',
            currency: {
                carrots: carrots,
                horseShoes: horseShoes,
                goldenCarrots: goldenCarrots,
                lastSyncAt: new Date().toISOString()
            }
        });

    } catch (error) {
        console.error('Currency sync error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to sync currency data'
        });
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
