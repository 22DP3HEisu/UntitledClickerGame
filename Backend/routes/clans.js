var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { sanitizeInput, validateLength, validateNotEmpty } = require('../lib/validation');

// GET /clans - Retrieve all clans
router.get('/', async function(req, res, next) {
    try {
        console.log('Fetching all clans...');

        // Query to get all clans with their leader information and member count
        const query = `
            SELECT 
                c.ClanID,
                c.ClanName,
                c.ClanTag,
                c.ClanDescription,
                c.CreationDate,
                u.Username as LeaderName,
                COUNT(cu.UserID) as MemberCount
            FROM Clans c
            LEFT JOIN Users u ON c.ClanLeaderID = u.UserID
            LEFT JOIN Clan_users cu ON c.ClanID = cu.ClanID
            GROUP BY c.ClanID, c.ClanName, c.ClanTag, c.ClanDescription, c.CreationDate, u.Username
            ORDER BY MemberCount DESC, c.CreationDate DESC
        `;

        const clans = await executeQuery(query);

        console.log(`Retrieved ${clans.length} clans`);

        res.json({
            success: true,
            message: 'All clans retrieved successfully',
            totalClans: clans.length,
            clans: clans.map(clan => ({
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag,
                description: clan.ClanDescription || '',
                leaderName: clan.LeaderName || 'Unknown',
                memberCount: clan.MemberCount,
                creationDate: clan.CreationDate
            }))
        });

    } catch (error) {
        console.error('Get all clans error:', error);
        res.status(500).json({ 
            success: false,
            error: 'Internal server error',
            message: 'Failed to retrieve clans'
        });
    }
});

// Helper function to generate random alphanumeric string
function generateRandomString(length) {
    const characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let result = '';
    for (let i = 0; i < length; i++) {
        result += characters.charAt(Math.floor(Math.random() * characters.length));
    }
    return result;
}

// Helper function to generate unique clan tag with hashtag
function generateClanTag() {
    // Generate 6-10 random alphanumeric characters
    const length = Math.floor(Math.random() * 5) + 6; // Random length between 6-10
    const randomString = generateRandomString(length);
    return '#' + randomString;
}

// Helper function to find available clan tag
async function findAvailableClanTag() {
    const maxAttempts = 100; // Prevent infinite loop (very unlikely to be needed)
    
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
        const tag = generateClanTag();
        
        // Check if tag is available
        const tagCheckQuery = 'SELECT ClanID FROM Clans WHERE ClanTag = ?';
        const tagExists = await executeQuery(tagCheckQuery, [tag]);
        
        if (tagExists.length === 0) {
            return tag; // Tag is available
        }
    }
    
    // Fallback (extremely unlikely to reach here)
    throw new Error('Unable to generate unique clan tag after multiple attempts');
}

// GET /clans/:id - Get specific clan information
router.get('/:id', async function(req, res, next) {
    try {
        console.log('Fetching specific clan information...');

        const clanId = parseInt(req.params.id);

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Query to get clan information with leader and member details
        const clanQuery = `
            SELECT 
                c.ClanID,
                c.ClanName,
                c.ClanTag,
                c.ClanDescription,
                c.CreationDate,
                u.Username as LeaderName,
                COUNT(cu.UserID) as MemberCount
            FROM Clans c
            LEFT JOIN Users u ON c.ClanLeaderID = u.UserID
            LEFT JOIN Clan_users cu ON c.ClanID = cu.ClanID
            WHERE c.ClanID = ?
            GROUP BY c.ClanID, c.ClanName, c.ClanTag, c.ClanDescription, c.CreationDate, u.Username
        `;

        const clanResult = await executeQuery(clanQuery, [clanId]);

        if (clanResult.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Clan not found',
                message: 'The specified clan does not exist'
            });
        }

        // Query to get clan members list
        const membersQuery = `
            SELECT 
                u.UserID,
                u.Username,
                u.CreatedAt as JoinedDate,
                CASE WHEN c.ClanLeaderID = u.UserID THEN true ELSE false END as IsLeader
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            LEFT JOIN Clans c ON cu.ClanID = c.ClanID
            WHERE cu.ClanID = ?
            ORDER BY IsLeader DESC, u.Username ASC
        `;

        const membersResult = await executeQuery(membersQuery, [clanId]);

        const clan = clanResult[0];

        console.log(`Retrieved clan information: ${clan.ClanName} (${clan.ClanTag}) with ${clan.MemberCount} members`);

        res.json({
            success: true,
            message: 'Clan information retrieved successfully',
            clan: {
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag,
                description: clan.ClanDescription || '',
                leaderName: clan.LeaderName || 'Unknown',
                memberCount: clan.MemberCount,
                creationDate: clan.CreationDate,
                members: membersResult.map(member => ({
                    id: member.UserID,
                    username: member.Username,
                    isLeader: member.IsLeader,
                    joinedDate: member.JoinedDate
                }))
            }
        });

    } catch (error) {
        console.error('Get clan information error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to retrieve clan information'
        });
    }
});

// POST /clans - Create a new clan
router.post('/', authenticateToken, async function(req, res, next) {
    try {
        console.log('Creating new clan...');

        const { clanName, clanDescription } = req.body;
        const userId = req.user.id;

        // Validation
        const errors = [];

        // Validate required fields
        if (!clanName) {
            errors.push('Clan name is required');
        }

        // Validate and sanitize clan name
        const sanitizedClanName = sanitizeInput(clanName);
        if (!validateNotEmpty(sanitizedClanName)) {
            errors.push('Clan name cannot be empty');
        } else if (!validateLength(sanitizedClanName, 3, 50)) {
            errors.push('Clan name must be between 3 and 50 characters');
        }

        // Validate and sanitize clan description (optional)
        let sanitizedClanDescription = null;
        if (clanDescription) {
            sanitizedClanDescription = sanitizeInput(clanDescription);
            if (!validateLength(sanitizedClanDescription, 0, 500)) {
                errors.push('Clan description cannot exceed 500 characters');
            }
        }

        if (errors.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Validation failed',
                message: 'Please fix the following errors',
                errors: errors
            });
        }

        // Check if user is already a clan leader
        const existingLeadershipQuery = 'SELECT ClanID FROM Clans WHERE ClanLeaderID = ?';
        const existingLeadership = await executeQuery(existingLeadershipQuery, [userId]);

        if (existingLeadership.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Already clan leader',
                message: 'You are already a leader of another clan'
            });
        }

        // Check if user is already a member of any clan
        const existingMembershipQuery = 'SELECT ClanID FROM Clan_users WHERE UserID = ?';
        const existingMembership = await executeQuery(existingMembershipQuery, [userId]);

        if (existingMembership.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Already clan member',
                message: 'You must leave your current clan before creating a new one'
            });
        }

        // Check if clan name already exists
        const nameCheckQuery = 'SELECT ClanID FROM Clans WHERE ClanName = ?';
        const nameExists = await executeQuery(nameCheckQuery, [sanitizedClanName]);

        if (nameExists.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Clan name taken',
                message: 'A clan with this name already exists'
            });
        }

        // Generate an available clan tag
        const generatedClanTag = await findAvailableClanTag();

        // Create the clan
        const createClanQuery = `
            INSERT INTO Clans (ClanName, ClanTag, ClanDescription, ClanLeaderID)
            VALUES (?, ?, ?, ?)
        `;

        const createResult = await executeQuery(createClanQuery, [
            sanitizedClanName,
            generatedClanTag,
            sanitizedClanDescription,
            userId
        ]);

        const clanId = createResult.insertId;

        // Add the creator as the first member
        const addMemberQuery = 'INSERT INTO Clan_users (ClanID, UserID) VALUES (?, ?)';
        await executeQuery(addMemberQuery, [clanId, userId]);

        // Get the newly created clan with leader info
        const newClanQuery = `
            SELECT 
                c.ClanID,
                c.ClanName,
                c.ClanTag,
                c.ClanDescription,
                c.CreationDate,
                u.Username as LeaderName,
                COUNT(cu.UserID) as MemberCount
            FROM Clans c
            LEFT JOIN Users u ON c.ClanLeaderID = u.UserID
            LEFT JOIN Clan_users cu ON c.ClanID = cu.ClanID
            WHERE c.ClanID = ?
            GROUP BY c.ClanID, c.ClanName, c.ClanTag, c.ClanDescription, c.CreationDate, u.Username
        `;

        const newClan = await executeQuery(newClanQuery, [clanId]);

        if (newClan.length === 0) {
            throw new Error('Failed to retrieve newly created clan');
        }

        const clan = newClan[0];

        console.log(`Clan created successfully: ${clan.ClanName} (${clan.ClanTag}) by user ${userId}`);

        res.status(201).json({
            success: true,
            message: 'Clan created successfully',
            clan: {
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag,
                description: clan.ClanDescription || '',
                leaderName: clan.LeaderName,
                memberCount: clan.MemberCount,
                creationDate: clan.CreationDate
            }
        });

    } catch (error) {
        console.error('Create clan error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to create clan'
        });
    }
});

// DELETE /clans/:id - Delete a clan (only by clan leader)
router.delete('/:id', authenticateToken, async function(req, res, next) {
    try {
        console.log('Deleting clan...');

        const clanId = parseInt(req.params.id);
        const userId = req.user.id;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Check if clan exists and get clan info
        const clanQuery = 'SELECT ClanID, ClanName, ClanTag, ClanLeaderID FROM Clans WHERE ClanID = ?';
        const clanResult = await executeQuery(clanQuery, [clanId]);

        if (clanResult.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Clan not found',
                message: 'The specified clan does not exist'
            });
        }

        const clan = clanResult[0];

        // Check if the requesting user is the clan leader
        if (clan.ClanLeaderID !== userId) {
            return res.status(403).json({
                success: false,
                error: 'Unauthorized',
                message: 'Only the clan leader can delete the clan'
            });
        }

        // Get member count for logging
        const memberCountQuery = 'SELECT COUNT(*) as memberCount FROM Clan_users WHERE ClanID = ?';
        const memberCountResult = await executeQuery(memberCountQuery, [clanId]);
        const memberCount = memberCountResult[0].memberCount;

        // Delete clan memberships first (foreign key constraint)
        const deleteMembersQuery = 'DELETE FROM Clan_users WHERE ClanID = ?';
        await executeQuery(deleteMembersQuery, [clanId]);

        // Delete the clan
        const deleteClanQuery = 'DELETE FROM Clans WHERE ClanID = ?';
        const deleteResult = await executeQuery(deleteClanQuery, [clanId]);

        if (deleteResult.affectedRows === 0) {
            throw new Error('Failed to delete clan from database');
        }

        console.log(`Clan deleted successfully: ${clan.ClanName} (${clan.ClanTag}) by user ${userId}, had ${memberCount} members`);

        res.json({
            success: true,
            message: 'Clan deleted successfully',
            deletedClan: {
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag,
                memberCount: memberCount
            }
        });

    } catch (error) {
        console.error('Delete clan error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to delete clan'
        });
    }
});

// POST /clans/:id/join - Join a clan
router.post('/:id/join', authenticateToken, async function(req, res, next) {
    try {
        console.log('User attempting to join clan...');

        const clanId = parseInt(req.params.id);
        const userId = req.user.id;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Check if clan exists
        const clanQuery = 'SELECT ClanID, ClanName, ClanTag FROM Clans WHERE ClanID = ?';
        const clanResult = await executeQuery(clanQuery, [clanId]);

        if (clanResult.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Clan not found',
                message: 'The specified clan does not exist'
            });
        }

        const clan = clanResult[0];

        // Check if user is already a member of any clan
        const existingMembershipQuery = 'SELECT ClanID FROM Clan_users WHERE UserID = ?';
        const existingMembership = await executeQuery(existingMembershipQuery, [userId]);

        if (existingMembership.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Already in clan',
                message: 'You must leave your current clan before joining another one'
            });
        }

        // Check if user is already a clan leader
        const existingLeadershipQuery = 'SELECT ClanID FROM Clans WHERE ClanLeaderID = ?';
        const existingLeadership = await executeQuery(existingLeadershipQuery, [userId]);

        if (existingLeadership.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Already clan leader',
                message: 'You are already a leader of another clan. You must delete your clan before joining another one'
            });
        }

        // Check if user is trying to join their own clan (shouldn't happen but safety check)
        const isLeaderQuery = 'SELECT ClanID FROM Clans WHERE ClanID = ? AND ClanLeaderID = ?';
        const isLeader = await executeQuery(isLeaderQuery, [clanId, userId]);

        if (isLeader.length > 0) {
            return res.status(400).json({
                success: false,
                error: 'Already in clan',
                message: 'You are already the leader of this clan'
            });
        }

        // Add user to the clan
        const joinClanQuery = 'INSERT INTO Clan_users (ClanID, UserID) VALUES (?, ?)';
        const joinResult = await executeQuery(joinClanQuery, [clanId, userId]);

        if (joinResult.affectedRows === 0) {
            throw new Error('Failed to join clan');
        }

        // Get updated clan information with member count
        const updatedClanQuery = `
            SELECT 
                c.ClanID,
                c.ClanName,
                c.ClanTag,
                c.ClanDescription,
                c.CreationDate,
                u.Username as LeaderName,
                COUNT(cu.UserID) as MemberCount
            FROM Clans c
            LEFT JOIN Users u ON c.ClanLeaderID = u.UserID
            LEFT JOIN Clan_users cu ON c.ClanID = cu.ClanID
            WHERE c.ClanID = ?
            GROUP BY c.ClanID, c.ClanName, c.ClanTag, c.ClanDescription, c.CreationDate, u.Username
        `;

        const updatedClan = await executeQuery(updatedClanQuery, [clanId]);

        if (updatedClan.length === 0) {
            throw new Error('Failed to retrieve updated clan information');
        }

        const clanInfo = updatedClan[0];

        console.log(`User ${userId} successfully joined clan: ${clan.ClanName} (${clan.ClanTag})`);

        res.json({
            success: true,
            message: 'Successfully joined clan',
            clan: {
                id: clanInfo.ClanID,
                name: clanInfo.ClanName,
                tag: clanInfo.ClanTag,
                description: clanInfo.ClanDescription || '',
                leaderName: clanInfo.LeaderName,
                memberCount: clanInfo.MemberCount,
                creationDate: clanInfo.CreationDate
            }
        });

    } catch (error) {
        console.error('Join clan error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to join clan'
        });
    }
});

module.exports = router;
