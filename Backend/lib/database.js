const mysql = require('mysql2');

// Load environment variables
require('dotenv').config();

// Database configuration from environment variables
const config = {
  host: process.env.DB_HOST || 'localhost',
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  port: process.env.DB_PORT || 3306,
  // Connection pool settings for better performance
  connectionLimit: 10,
  queueLimit: 0,
  multipleStatements: true
};

// Create connection pool (better than single connection)
const pool = mysql.createPool(config);

// Promisify for async/await support
const promisePool = pool.promise();

// Helper function to execute queries
async function executeQuery(query, params = []) {
  try {
    const [rows] = await promisePool.execute(query, params);
    return rows;
  } catch (error) {
    console.error('Database query error:', error);
    throw error;
  }
}

// Helper function to execute raw queries (for migrations)
async function executeRawQuery(query, params = []) {
  try {
    const [rows, fields] = await promisePool.query(query, params);
    return rows;
  } catch (error) {
    console.error('Database raw query error:', error);
    throw error;
  }
}

// Test database connection
async function testConnection() {
  try {
    const [rows] = await promisePool.execute('SELECT 1 as test');
    console.log('Database connection successful');
    return true;
  } catch (error) {
    console.error('Database connection failed:', error);
    return false;
  }
}

// Graceful shutdown
function closeConnection() {
  return new Promise((resolve) => {
    pool.end(() => {
      console.log('Database pool closed');
      resolve();
    });
  });
}

module.exports = {
  pool,
  promisePool,
  executeQuery,
  executeRawQuery,
  testConnection,
  closeConnection
};