var bcrypt = require('bcrypt');
var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { generateToken } = require('../lib/auth');

// User login endpoint
router.post('/login', async function(req, res, next) {
    const { username, password } = req.body;

    // Basic validation
    if (!username || !password) {
        return res.status(400).json({ 
            error: 'Missing credentials',
            message: 'Username and password are required'
        });
    }

    try {
        // Find user by username or email
        const query = 'SELECT UserID, Username, Email, PasswordHash FROM Users WHERE Username = ? OR Email = ?';
        const users = await executeQuery(query, [username, username]);

        if (users.length === 0) {
            return res.status(401).json({ 
                error: 'Invalid credentials',
                message: 'Username or password is incorrect'
            });
        }

        const user = users[0];

        // Verify password
        const isValidPassword = await bcrypt.compare(password, user.PasswordHash);
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
    console.log(req.body);
    

    // Basic validation
    if (!username || !email || !password) {
        return res.status(400).json({ error: 'All fields are required' });
    }

    // Hash password (you should use a library like bcrypt)
    const passwordHash = await bcrypt.hash(password, 10);

    // Insert user into database
    const query = 'INSERT INTO Users (Username, Email, PasswordHash) VALUES (?, ?, ?)';
    executeQuery(query, [username, email, passwordHash])
        .then(result => {
            // Create user object for token generation
            const newUser = {
                UserID: result.insertId,
                Username: username,
                Email: email
            };

            // Generate JWT token
            const token = generateToken(newUser);

            // Return user info with token
            res.status(201).json({ 
                message: 'User created successfully',
                user: {
                    id: result.insertId,
                    username,
                    email
                },
                token,
                expiresIn: '7d'
            });
        })
        .catch(err => {
            console.error('Error inserting user:', err);
            
            // Check for duplicate username/email
            if (err.code === 'ER_DUP_ENTRY') {
                return res.status(409).json({ 
                    error: 'User already exists',
                    message: 'Username or email already taken'
                });
            }
            
            res.status(500).json({ error: 'Internal server error' });
        });
});

module.exports = router;