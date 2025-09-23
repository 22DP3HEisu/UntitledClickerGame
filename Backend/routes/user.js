var bcrypt = require('bcrypt');
var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');

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

module.exports = router;
