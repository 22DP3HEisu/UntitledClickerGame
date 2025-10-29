var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { sanitizeInput, validateLength, validateNotEmpty } = require('../lib/validation');

// GET /chat/clan/:clanId - Get clan chat messages
router.get('/clan/:clanId', authenticateToken, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);
        const userId = req.user.id;
        const { limit = 50, offset = 0 } = req.query;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Check if user is a member of this clan
        const membershipQuery = 'SELECT ClanRank FROM Clan_users WHERE ClanID = ? AND UserID = ?';
        const membershipResult = await executeQuery(membershipQuery, [clanId, userId]);

        if (membershipResult.length === 0) {
            return res.status(403).json({
                success: false,
                error: 'Access denied',
                message: 'You must be a member of this clan to view chat messages'
            });
        }

        // Get chat messages with user info
        const chatQuery = `
            SELECT 
                cm.MessageID,
                cm.Message,
                cm.MessageType,
                cm.Timestamp,
                u.Username,
                cu.ClanRank,
                CASE WHEN c.ClanLeaderID = u.UserID THEN true ELSE false END as IsLeader
            FROM Clan_messages cm
            JOIN Users u ON cm.UserID = u.UserID
            LEFT JOIN Clan_users cu ON cm.ClanID = cu.ClanID AND cm.UserID = cu.UserID
            LEFT JOIN Clans c ON cm.ClanID = c.ClanID
            WHERE cm.ClanID = ?
            ORDER BY cm.Timestamp DESC
            LIMIT ? OFFSET ?
        `;

        const messages = await executeQuery(chatQuery, [clanId, parseInt(limit), parseInt(offset)]);

        // Reverse to get chronological order (oldest first)
        const chronologicalMessages = messages.reverse();

        res.json({
            success: true,
            message: 'Chat messages retrieved successfully',
            messages: chronologicalMessages.map(msg => ({
                id: msg.MessageID,
                message: msg.Message,
                messageType: msg.MessageType || 'chat',
                timestamp: msg.Timestamp,
                user: {
                    username: msg.Username,
                    rank: msg.ClanRank || 'Member',
                    isLeader: !!msg.IsLeader
                }
            })),
            pagination: {
                limit: parseInt(limit),
                offset: parseInt(offset),
                total: chronologicalMessages.length
            }
        });

    } catch (error) {
        console.error('Get clan chat error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to retrieve chat messages'
        });
    }
});

// POST /chat/clan/:clanId - Send a message to clan chat
router.post('/clan/:clanId', authenticateToken, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);
        const userId = req.user.id;
        const { message, messageType = 'chat' } = req.body;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Validate message
        if (!message || typeof message !== 'string') {
            return res.status(400).json({
                success: false,
                error: 'Invalid message',
                message: 'Message is required and must be a string'
            });
        }

        // Sanitize and validate message
        const sanitizedMessage = sanitizeInput(message.trim());
        if (!validateNotEmpty(sanitizedMessage)) {
            return res.status(400).json({
                success: false,
                error: 'Empty message',
                message: 'Message cannot be empty'
            });
        }

        if (!validateLength(sanitizedMessage, 1, 500)) {
            return res.status(400).json({
                success: false,
                error: 'Message too long',
                message: 'Message must be between 1 and 500 characters'
            });
        }

        // Validate message type
        const validMessageTypes = ['chat', 'system', 'join', 'leave', 'promotion'];
        if (!validMessageTypes.includes(messageType)) {
            return res.status(400).json({
                success: false,
                error: 'Invalid message type',
                message: 'Message type must be one of: ' + validMessageTypes.join(', ')
            });
        }

        // Check if user is a member of this clan
        const membershipQuery = `
            SELECT cu.ClanRank, u.Username
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            WHERE cu.ClanID = ? AND cu.UserID = ?
        `;
        const membershipResult = await executeQuery(membershipQuery, [clanId, userId]);

        if (membershipResult.length === 0) {
            return res.status(403).json({
                success: false,
                error: 'Access denied',
                message: 'You must be a member of this clan to send messages'
            });
        }

        const userRank = membershipResult[0].ClanRank;
        const username = membershipResult[0].Username;

        // Check for rate limiting (optional - prevent spam)
        const rateLimitQuery = `
            SELECT COUNT(*) as recentMessages 
            FROM Clan_messages 
            WHERE UserID = ? AND ClanID = ? AND Timestamp > DATE_SUB(NOW(), INTERVAL 1 MINUTE)
        `;
        const rateLimitResult = await executeQuery(rateLimitQuery, [userId, clanId]);
        const recentMessageCount = rateLimitResult[0].recentMessages;

        if (recentMessageCount >= 10) {
            return res.status(429).json({
                success: false,
                error: 'Rate limit exceeded',
                message: 'You can only send 10 messages per minute'
            });
        }

        // Insert the message
        const insertMessageQuery = `
            INSERT INTO Clan_messages (ClanID, UserID, Message, MessageType, Timestamp)
            VALUES (?, ?, ?, ?, NOW())
        `;
        const insertResult = await executeQuery(insertMessageQuery, [clanId, userId, sanitizedMessage, messageType]);

        if (insertResult.affectedRows === 0) {
            throw new Error('Failed to insert message');
        }

        // Get the inserted message with user info
        const newMessageQuery = `
            SELECT 
                cm.MessageID,
                cm.Message,
                cm.MessageType,
                cm.Timestamp,
                u.Username,
                cu.ClanRank,
                CASE WHEN c.ClanLeaderID = u.UserID THEN true ELSE false END as IsLeader
            FROM Clan_messages cm
            JOIN Users u ON cm.UserID = u.UserID
            LEFT JOIN Clan_users cu ON cm.ClanID = cu.ClanID AND cm.UserID = cu.UserID
            LEFT JOIN Clans c ON cm.ClanID = c.ClanID
            WHERE cm.MessageID = ?
        `;
        const newMessage = await executeQuery(newMessageQuery, [insertResult.insertId]);

        if (newMessage.length === 0) {
            throw new Error('Failed to retrieve new message');
        }

        const msg = newMessage[0];

        console.log(`Chat message sent by ${username} (${userRank}) in clan ${clanId}: "${sanitizedMessage}"`);

        res.status(201).json({
            success: true,
            message: 'Message sent successfully',
            chatMessage: {
                id: msg.MessageID,
                message: msg.Message,
                messageType: msg.MessageType,
                timestamp: msg.Timestamp,
                user: {
                    username: msg.Username,
                    rank: msg.ClanRank,
                    isLeader: !!msg.IsLeader
                }
            }
        });

    } catch (error) {
        console.error('Send clan chat message error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to send message'
        });
    }
});

// POST /chat/clan/:clanId/system - Send system message (for clan events)
router.post('/clan/:clanId/system', authenticateToken, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);
        const userId = req.user.id;
        const { message, eventType = 'system' } = req.body;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Check if user has permission to send system messages (Leader or Officer)
        const membershipQuery = 'SELECT ClanRank FROM Clan_users WHERE ClanID = ? AND UserID = ?';
        const membershipResult = await executeQuery(membershipQuery, [clanId, userId]);

        if (membershipResult.length === 0) {
            return res.status(403).json({
                success: false,
                error: 'Access denied',
                message: 'You must be a member of this clan'
            });
        }

        const userRank = membershipResult[0].ClanRank;
        if (userRank !== 'Leader' && userRank !== 'Officer') {
            return res.status(403).json({
                success: false,
                error: 'Insufficient permissions',
                message: 'Only Leaders and Officers can send system messages'
            });
        }

        // Validate and sanitize message
        const sanitizedMessage = sanitizeInput(message);
        if (!validateNotEmpty(sanitizedMessage) || !validateLength(sanitizedMessage, 1, 500)) {
            return res.status(400).json({
                success: false,
                error: 'Invalid message',
                message: 'Message must be between 1 and 500 characters'
            });
        }

        // Insert system message
        const insertMessageQuery = `
            INSERT INTO Clan_messages (ClanID, UserID, Message, MessageType, Timestamp)
            VALUES (?, ?, ?, ?, NOW())
        `;
        await executeQuery(insertMessageQuery, [clanId, userId, sanitizedMessage, eventType]);

        res.json({
            success: true,
            message: 'System message sent successfully'
        });

    } catch (error) {
        console.error('Send system message error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to send system message'
        });
    }
});

module.exports = router;