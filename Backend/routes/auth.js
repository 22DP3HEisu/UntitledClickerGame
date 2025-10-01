var bcrypt = require('bcrypt');
var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { generateToken } = require('../lib/auth');
const { validateUserLogin, validateUserRegistration } = require('../lib/validation');

// User login endpoint
router.post('/login', async function(req, res, next) {
    const { username, password } = req.body;

    // Validate input using centralized validation
    const validation = validateUserLogin({ username, password });
    if (!validation.success) {
        return res.status(400).json({
            error: 'Invalid credentials',
            message: validation.errors[0] // Return first error
        });
    }

    const { username: sanitizedUsername, password: sanitizedPassword } = validation.sanitizedData;

    try {
        // Find user by username or email
        const query = 'SELECT UserID, Username, Email, PasswordHash FROM Users WHERE Username = ? OR Email = ?';
        const users = await executeQuery(query, [sanitizedUsername, sanitizedUsername]);

        if (users.length === 0) {
            return res.status(401).json({ 
                error: 'Invalid credentials',
                message: 'Username or password is incorrect'
            });
        }

        const user = users[0];

        // Verify password
        const isValidPassword = await bcrypt.compare(sanitizedPassword, user.PasswordHash);
        if (!isValidPassword) {
            return res.status(401).json({ 
                error: 'Invalid credentials',
                message: 'Username or password is incorrect'
            });
        }

        // Generate JWT token
        const token = generateToken(user);

        // Return user info with token (don't include password hash)
        res.json({ 
            message: 'Login successful',
            user: {
                id: user.UserID,
                username: user.Username,
                email: user.Email
            },
            token,
            expiresIn: '7d'
        });

    } catch (error) {
        console.error('Login error:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// User registration endpoint
router.post('/register', async function(req, res, next) {
    const { username, email, password } = req.body;

    // Validate input using centralized validation
    const validation = validateUserRegistration({ username, email, password });
    if (!validation.success) {
        return res.status(400).json({
            error: 'Invalid input',
            message: validation.errors[0] // Return first error
        });
    }

    const { username: sanitizedUsername, email: sanitizedEmail, password: sanitizedPassword } = validation.sanitizedData;

    try {
        // Hash password with bcrypt
        const saltRounds = 12; // Increased from 10 for better security
        const passwordHash = await bcrypt.hash(sanitizedPassword, saltRounds);

        // Insert user into database
        const query = 'INSERT INTO Users (Username, Email, PasswordHash) VALUES (?, ?, ?)';
        const result = await executeQuery(query, [sanitizedUsername, sanitizedEmail, passwordHash]);

        // Create user object for token generation
        const newUser = {
            UserID: result.insertId,
            Username: sanitizedUsername,
            Email: sanitizedEmail
        };

        // Generate JWT token
        const token = generateToken(newUser);

        // Return user info with token
        res.status(201).json({ 
            message: 'User created successfully',
            user: {
                id: result.insertId,
                username: sanitizedUsername,
                email: sanitizedEmail
            },
            token,
            expiresIn: '7d'
        });

    } catch (err) {
        console.error('Error during registration:', err);
        
        // Check for duplicate username/email
        if (err.code === 'ER_DUP_ENTRY') {
            return res.status(409).json({ 
                error: 'User already exists',
                message: 'Username or email already taken'
            });
        }
        
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to create user account'
        });
    }
});

module.exports = router;