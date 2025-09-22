const mysql = require('mysql2');
const fs = require('fs');
const path = require('path');

// Load environment variables
require('dotenv').config();

// Database configuration from environment variables
const config = {
  host: process.env.DB_HOST || 'localhost',
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  port: process.env.DB_PORT || 3306
};

// Create connection
const connection = mysql.createConnection(config);

// Function to run migrations
async function runMigrations() {
  try {
    // Connect to database
    connection.connect((err) => {
      if (err) {
        console.error('Error connecting to database:', err);
        return;
      }
      console.log('Connected to MySQL database');
    });

    // Create migrations table to track executed migrations
    const createMigrationsTable = `
      CREATE TABLE IF NOT EXISTS migrations (
        id INT PRIMARY KEY AUTO_INCREMENT,
        filename VARCHAR(255) NOT NULL UNIQUE,
        executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    
    await executeQuery(createMigrationsTable);
    console.log('Migrations table ready');

    // Read migration files
    const migrationsDir = path.join(__dirname, '../migrations');
    const files = fs.readdirSync(migrationsDir)
      .filter(file => file.endsWith('.sql'))
      .sort(); // Execute in alphabetical order

    console.log(`Found ${files.length} migration files`);

    // Check which migrations have already been executed
    const executedMigrations = await executeQuery('SELECT filename FROM migrations');
    const executedFiles = executedMigrations.map(row => row.filename);

    // Execute pending migrations
    for (const file of files) {
      if (executedFiles.includes(file)) {
        console.log(`Skipping ${file} (already executed)`);
        continue;
      }

      console.log(`Executing migration: ${file}`);
      
      // Read and execute SQL file
      const sqlContent = fs.readFileSync(path.join(migrationsDir, file), 'utf8');
      
      // Split by semicolon to handle multiple statements
      const statements = sqlContent.split(';').filter(stmt => stmt.trim());
      
      for (const statement of statements) {
        if (statement.trim()) {
          await executeQuery(statement);
        }
      }

      // Mark migration as executed
      await executeQuery('INSERT INTO migrations (filename) VALUES (?)', [file]);
      console.log(`✓ Migration ${file} completed`);
    }

    console.log('All migrations completed successfully!');
    
  } catch (error) {
    console.error('Migration failed:', error);
  } finally {
    connection.end();
  }
}

// Helper function to promisify MySQL queries
function executeQuery(query, params = []) {
  return new Promise((resolve, reject) => {
    connection.query(query, params, (error, results) => {
      if (error) {
        reject(error);
      } else {
        resolve(results);
      }
    });
  });
}

// Run migrations
if (require.main === module) {
  runMigrations();
}

module.exports = { runMigrations };