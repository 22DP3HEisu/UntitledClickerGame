const jwt = require('jsonwebtoken');

// Load environment variables
require('dotenv').config();

const JWT_SECRET = process.env.JWT_SECRET || 'your_jwt_secret_here';
const JWT_EXPIRES_IN = process.env.JWT_EXPIRES_IN || '7d';

/**
 * Generate a JWT token for a user
 * @param {Object} user - User object containing id, username, email
 * @returns {string} JWT token
 */
function generateToken(user) {
  const payload = {
    id: user.UserID || user.id,
    username: user.Username || user.username,
    email: user.Email || user.email
  };

  return jwt.sign(payload, JWT_SECRET, {
    expiresIn: JWT_EXPIRES_IN,
    issuer: 'clickergame-backend'
  });
}

/**
 * Verify a JWT token
 * @param {string} token - JWT token to verify
 * @returns {Object} Decoded token payload
 */
function verifyToken(token) {
  try {
    return jwt.verify(token, JWT_SECRET);
  } catch (error) {
    throw new Error('Invalid or expired token');
  }
}

/**
 * Middleware to authenticate requests
 * Checks for Bearer token in Authorization header
 */
function authenticateToken(req, res, next) {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1]; // Bearer TOKEN

  if (!token) {
    return res.status(401).json({ 
      error: 'Access token required',
      message: 'Please provide a valid authentication token'
    });
  }

  try {
    const decoded = verifyToken(token);
    req.user = decoded; // Add user info to request object
    next();
  } catch (error) {
    return res.status(403).json({ 
      error: 'Invalid token',
      message: error.message
    });
  }
}

/**
 * Optional authentication middleware
 * Adds user info if token is provided, but doesn't require it
 */
function optionalAuth(req, res, next) {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1];

  if (token) {
    try {
      const decoded = verifyToken(token);
      req.user = decoded;
    } catch (error) {
      // Token is invalid but we don't block the request
      req.user = null;
    }
  } else {
    req.user = null;
  }
  
  next();
}

/**
 * Generate a refresh token (for future use)
 * @param {Object} user - User object
 * @returns {string} Refresh token
 */
function generateRefreshToken(user) {
  const payload = {
    id: user.UserID || user.id,
    type: 'refresh'
  };

  return jwt.sign(payload, JWT_SECRET, {
    expiresIn: '30d',
    issuer: 'clickergame-backend'
  });
}

module.exports = {
  generateToken,
  verifyToken,
  authenticateToken,
  optionalAuth,
  generateRefreshToken
};