-- Migration: Add Clan Chat System
-- This creates the necessary table for clan chat functionality

-- Create Clan_messages table
CREATE TABLE IF NOT EXISTS Clan_messages (
    MessageID INT AUTO_INCREMENT PRIMARY KEY,
    ClanID INT NOT NULL,
    UserID INT NOT NULL,
    Message TEXT NOT NULL,
    MessageType ENUM('chat', 'system', 'join', 'leave', 'promotion', 'kick') DEFAULT 'chat',
    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraints
    FOREIGN KEY (ClanID) REFERENCES Clans(ClanID) ON DELETE CASCADE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    
    -- Indexes for performance
    INDEX idx_clan_timestamp (ClanID, Timestamp),
    INDEX idx_user_clan (UserID, ClanID),
    INDEX idx_timestamp (Timestamp)
);

-- Add some sample system messages for existing clans (optional)
-- This can be commented out if you don't want sample data
/*
INSERT INTO Clan_messages (ClanID, UserID, Message, MessageType) 
SELECT 
    c.ClanID,
    c.ClanLeaderID,
    CONCAT('Welcome to ', c.ClanName, '! Chat is now available.'),
    'system'
FROM Clans c
WHERE c.ClanID IN (SELECT DISTINCT ClanID FROM Clans LIMIT 5);
*/