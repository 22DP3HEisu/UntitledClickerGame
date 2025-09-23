const fs = require('fs');
const path = require('path');
const { executeQuery, executeRawQuery, testConnection, closeConnection } = require('../lib/database');

// Function to run migrations
async function runMigrations() {
  try {
    // Test database connection
    const connected = await testConnection();
    if (!connected) {
      console.error('Failed to connect to database');
      return;
    }

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
          await executeRawQuery(statement);
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
    await closeConnection();
  }
}

// Run migrations
if (require.main === module) {
  runMigrations();
}

module.exports = { runMigrations };