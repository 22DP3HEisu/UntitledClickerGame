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

        // Add the creator as the first member with Leader rank
        const addMemberQuery = 'INSERT INTO Clan_users (ClanID, UserID, ClanRank) VALUES (?, ?, ?)';
        await executeQuery(addMemberQuery, [clanId, userId, 'Leader']);

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

// POST /clans/:id/kick - Kick a user from the clan (Officer+ only)
router.post('/:id/kick', authenticateToken, async function(req, res, next) {
    try {
        console.log('User attempting to kick member from clan...');

        const clanId = parseInt(req.params.id);
        const kickerId = req.user.id;
        const { userId: targetUserId } = req.body;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Validate target user ID
        if (!targetUserId || targetUserId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid user ID',
                message: 'Please provide a valid user ID to kick'
            });
        }

        // Prevent self-kick
        if (kickerId === targetUserId) {
            return res.status(400).json({
                success: false,
                error: 'Cannot kick yourself',
                message: 'You cannot kick yourself from the clan'
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

        // Get kicker's rank and membership status
        const kickerQuery = 'SELECT ClanRank FROM Clan_users WHERE ClanID = ? AND UserID = ?';
        const kickerResult = await executeQuery(kickerQuery, [clanId, kickerId]);

        if (kickerResult.length === 0) {
            return res.status(403).json({
                success: false,
                error: 'Not a clan member',
                message: 'You must be a member of this clan to kick users'
            });
        }

        const kickerRank = kickerResult[0].ClanRank;

        // Check if kicker has sufficient permissions (Officer or Leader)
        if (kickerRank !== 'Officer' && kickerRank !== 'Leader') {
            return res.status(403).json({
                success: false,
                error: 'Insufficient permissions',
                message: 'Only Officers and Leaders can kick clan members'
            });
        }

        // Get target user's rank and membership status
        const targetQuery = `
            SELECT cu.ClanRank, u.Username 
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            WHERE cu.ClanID = ? AND cu.UserID = ?
        `;
        const targetResult = await executeQuery(targetQuery, [clanId, targetUserId]);

        if (targetResult.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Target user not found',
                message: 'The specified user is not a member of this clan'
            });
        }

        const targetRank = targetResult[0].ClanRank;
        const targetUsername = targetResult[0].Username;

        // Define rank hierarchy (higher number = higher rank)
        const rankHierarchy = {
            'Member': 1,
            'Officer': 2,
            'Leader': 3
        };

        // Check if kicker can kick the target (must have higher rank)
        if (rankHierarchy[kickerRank] <= rankHierarchy[targetRank]) {
            return res.status(403).json({
                success: false,
                error: 'Cannot kick higher or equal rank',
                message: `You cannot kick users with ${targetRank} rank or higher`
            });
        }

        // Remove the target user from the clan
        const kickQuery = 'DELETE FROM Clan_users WHERE ClanID = ? AND UserID = ?';
        const kickResult = await executeQuery(kickQuery, [clanId, targetUserId]);

        if (kickResult.affectedRows === 0) {
            throw new Error('Failed to kick user from clan');
        }

        // Get updated clan information
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
        const clanInfo = updatedClan[0] || {};

        console.log(`User ${targetUserId} (${targetUsername}) kicked from clan ${clan.ClanName} by ${kickerId} (${kickerRank})`);

        res.json({
            success: true,
            message: 'User successfully kicked from clan',
            kickedUser: {
                id: targetUserId,
                username: targetUsername,
                rank: targetRank
            },
            clan: {
                id: clanInfo.ClanID,
                name: clanInfo.ClanName,
                tag: clanInfo.ClanTag,
                memberCount: clanInfo.MemberCount
            }
        });

    } catch (error) {
        console.error('Kick user error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to kick user from clan'
        });
    }
});

// POST /clans/:id/promote - Promote a user to a higher rank (Leader only)
router.post('/:id/promote', authenticateToken, async function(req, res, next) {
    try {
        console.log('User attempting to promote clan member...');

        const clanId = parseInt(req.params.id);
        const promoterId = req.user.id;
        const { userId: targetUserId, newRank } = req.body;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Validate target user ID
        if (!targetUserId || targetUserId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid user ID',
                message: 'Please provide a valid user ID to promote'
            });
        }

        // Validate new rank
        const validRanks = ['Member', 'Officer'];
        if (!newRank || !validRanks.includes(newRank)) {
            return res.status(400).json({
                success: false,
                error: 'Invalid rank',
                message: 'New rank must be either "Member" or "Officer"'
            });
        }

        // Prevent self-promotion
        if (promoterId === targetUserId) {
            return res.status(400).json({
                success: false,
                error: 'Cannot promote yourself',
                message: 'You cannot change your own rank'
            });
        }

        // Check if clan exists and if promoter is the leader
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

        // Check if promoter is the clan leader
        if (clan.ClanLeaderID !== promoterId) {
            return res.status(403).json({
                success: false,
                error: 'Insufficient permissions',
                message: 'Only the clan leader can promote members'
            });
        }

        // Get target user's current rank and membership status
        const targetQuery = `
            SELECT cu.ClanRank, u.Username 
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            WHERE cu.ClanID = ? AND cu.UserID = ?
        `;
        const targetResult = await executeQuery(targetQuery, [clanId, targetUserId]);

        if (targetResult.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Target user not found',
                message: 'The specified user is not a member of this clan'
            });
        }

        const currentRank = targetResult[0].ClanRank;
        const targetUsername = targetResult[0].Username;

        // Check if user is already at the target rank
        if (currentRank === newRank) {
            return res.status(400).json({
                success: false,
                error: 'Already at rank',
                message: `User is already at ${newRank} rank`
            });
        }

        // Update user's rank
        const promoteQuery = 'UPDATE Clan_users SET ClanRank = ? WHERE ClanID = ? AND UserID = ?';
        const promoteResult = await executeQuery(promoteQuery, [newRank, clanId, targetUserId]);

        if (promoteResult.affectedRows === 0) {
            throw new Error('Failed to update user rank');
        }

        console.log(`User ${targetUserId} (${targetUsername}) promoted from ${currentRank} to ${newRank} in clan ${clan.ClanName} by leader ${promoterId}`);

        res.json({
            success: true,
            message: 'User successfully promoted',
            promotedUser: {
                id: targetUserId,
                username: targetUsername,
                oldRank: currentRank,
                newRank: newRank
            },
            clan: {
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag
            }
        });

    } catch (error) {
        console.error('Promote user error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to promote user'
        });
    }
});

// POST /clans/:id/transfer-leadership - Transfer clan leadership to another user (Leader only)
router.post('/:id/transfer-leadership', authenticateToken, async function(req, res, next) {
    try {
        console.log('User attempting to transfer clan leadership...');

        const clanId = parseInt(req.params.id);
        const currentLeaderId = req.user.id;
        const { userId: newLeaderId } = req.body;

        // Validate clan ID
        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid clan ID',
                message: 'Please provide a valid clan ID'
            });
        }

        // Validate new leader user ID
        if (!newLeaderId || newLeaderId <= 0) {
            return res.status(400).json({
                success: false,
                error: 'Invalid user ID',
                message: 'Please provide a valid user ID for the new leader'
            });
        }

        // Prevent self-transfer (redundant but good for clarity)
        if (currentLeaderId === newLeaderId) {
            return res.status(400).json({
                success: false,
                error: 'Cannot transfer to yourself',
                message: 'You are already the leader of this clan'
            });
        }

        // Check if clan exists and if user is the current leader
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

        // Check if user is the current clan leader
        if (clan.ClanLeaderID !== currentLeaderId) {
            return res.status(403).json({
                success: false,
                error: 'Insufficient permissions',
                message: 'Only the current clan leader can transfer leadership'
            });
        }

        // Get new leader's membership status and current rank
        const newLeaderQuery = `
            SELECT cu.ClanRank, u.Username 
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            WHERE cu.ClanID = ? AND cu.UserID = ?
        `;
        const newLeaderResult = await executeQuery(newLeaderQuery, [clanId, newLeaderId]);

        if (newLeaderResult.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Target user not found',
                message: 'The specified user is not a member of this clan'
            });
        }

        const newLeaderUsername = newLeaderResult[0].Username;

        // Get current leader's username for logging
        const currentLeaderQuery = 'SELECT Username FROM Users WHERE UserID = ?';
        const currentLeaderResult = await executeQuery(currentLeaderQuery, [currentLeaderId]);
        const currentLeaderUsername = currentLeaderResult[0]?.Username || 'Unknown';

        // Begin transaction-like operations
        try {
            // Update the clan's leader in the Clans table
            const updateClanLeaderQuery = 'UPDATE Clans SET ClanLeaderID = ? WHERE ClanID = ?';
            await executeQuery(updateClanLeaderQuery, [newLeaderId, clanId]);

            // Update the new leader's rank to Leader in Clan_users table
            const updateNewLeaderRankQuery = 'UPDATE Clan_users SET ClanRank = ? WHERE ClanID = ? AND UserID = ?';
            await executeQuery(updateNewLeaderRankQuery, ['Leader', clanId, newLeaderId]);

            // Update the old leader's rank to Officer in Clan_users table
            const updateOldLeaderRankQuery = 'UPDATE Clan_users SET ClanRank = ? WHERE ClanID = ? AND UserID = ?';
            await executeQuery(updateOldLeaderRankQuery, ['Officer', clanId, currentLeaderId]);

        } catch (transactionError) {
            console.error('Transaction error during leadership transfer:', transactionError);
            throw new Error('Failed to complete leadership transfer');
        }

        console.log(`Clan leadership transferred from ${currentLeaderId} (${currentLeaderUsername}) to ${newLeaderId} (${newLeaderUsername}) in clan ${clan.ClanName}`);

        res.json({
            success: true,
            message: 'Clan leadership successfully transferred',
            transfer: {
                oldLeader: {
                    id: currentLeaderId,
                    username: currentLeaderUsername,
                    newRank: 'Officer'
                },
                newLeader: {
                    id: newLeaderId,
                    username: newLeaderUsername,
                    newRank: 'Leader'
                }
            },
            clan: {
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag
            }
        });

    } catch (error) {
        console.error('Transfer leadership error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to transfer clan leadership'
        });
    }
});

// POST /clans/:id/leave - Leave a clan
router.post('/:id/leave', authenticateToken, async function(req, res, next) {
    try {
        console.log('User attempting to leave clan...');

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

        // Check if user is a member of this clan
        const membershipQuery = `
            SELECT cu.ClanRank, u.Username 
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            WHERE cu.ClanID = ? AND cu.UserID = ?
        `;
        const membershipResult = await executeQuery(membershipQuery, [clanId, userId]);

        if (membershipResult.length === 0) {
            return res.status(400).json({
                success: false,
                error: 'Not a clan member',
                message: 'You are not a member of this clan'
            });
        }

        const userRank = membershipResult[0].ClanRank;
        const username = membershipResult[0].Username;

        // Check if user is the clan leader
        if (clan.ClanLeaderID === userId) {
            // Get member count to check if leader is the only member
            const memberCountQuery = 'SELECT COUNT(*) as memberCount FROM Clan_users WHERE ClanID = ?';
            const memberCountResult = await executeQuery(memberCountQuery, [clanId]);
            const memberCount = memberCountResult[0].memberCount;

            if (memberCount > 1) {
                return res.status(400).json({
                    success: false,
                    error: 'Cannot leave as leader',
                    message: 'You cannot leave the clan as a leader. Transfer leadership to another member first or disband the clan if you are the only member'
                });
            }

            // If leader is the only member, delete the entire clan
            try {
                // Delete clan memberships first (foreign key constraint)
                const deleteMembersQuery = 'DELETE FROM Clan_users WHERE ClanID = ?';
                await executeQuery(deleteMembersQuery, [clanId]);

                // Delete the clan
                const deleteClanQuery = 'DELETE FROM Clans WHERE ClanID = ?';
                await executeQuery(deleteClanQuery, [clanId]);

                console.log(`Clan ${clan.ClanName} (${clan.ClanTag}) automatically disbanded as leader ${userId} (${username}) was the only member`);

                return res.json({
                    success: true,
                    message: 'Successfully left clan and disbanded it as you were the only member',
                    action: 'disbanded',
                    clan: {
                        id: clan.ClanID,
                        name: clan.ClanName,
                        tag: clan.ClanTag
                    }
                });

            } catch (disbandError) {
                console.error('Error disbanding clan during leader leave:', disbandError);
                throw new Error('Failed to disband clan');
            }
        }

        // Remove user from the clan (regular member or officer)
        const leaveQuery = 'DELETE FROM Clan_users WHERE ClanID = ? AND UserID = ?';
        const leaveResult = await executeQuery(leaveQuery, [clanId, userId]);

        if (leaveResult.affectedRows === 0) {
            throw new Error('Failed to remove user from clan');
        }

        // Get updated clan information
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
        const clanInfo = updatedClan[0] || {};

        console.log(`User ${userId} (${username}) with rank ${userRank} left clan ${clan.ClanName} (${clan.ClanTag})`);

        res.json({
            success: true,
            message: 'Successfully left the clan',
            action: 'left',
            leftUser: {
                id: userId,
                username: username,
                rank: userRank
            },
            clan: {
                id: clanInfo.ClanID,
                name: clanInfo.ClanName,
                tag: clanInfo.ClanTag,
                memberCount: clanInfo.MemberCount
            }
        });

    } catch (error) {
        console.error('Leave clan error:', error);
        res.status(500).json({
            success: false,
            error: 'Internal server error',
            message: 'Failed to leave clan'
        });
    }
});

module.exports = router;
