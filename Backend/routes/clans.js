var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { sanitizeInput, validateLength, validateNotEmpty, validateAlphanumeric } = require('../lib/validation');

// Clan validation functions
function validateClanName(name) {
    if (typeof name !== 'string') return false;
    const sanitized = sanitizeInput(name);
    return validateNotEmpty(sanitized) && validateLength(sanitized, 3, 255);
}

function validateClanTag(tag) {
    if (typeof tag !== 'string') return false;
    const sanitized = sanitizeInput(tag);
    // Clan tag: 2-10 chars, alphanumeric only
    const tagRegex = /^[A-Za-z0-9]{2,10}$/;
    return tagRegex.test(sanitized);
}

function validateClanDescription(description) {
    if (typeof description !== 'string') return false;
    const sanitized = sanitizeInput(description);
    return validateLength(sanitized, 0, 500);
}

function validateClanRank(rank) {
    const validRanks = ['Member', 'Officer', 'Leader'];
    return validRanks.includes(rank);
}

// Get all clans (public route with pagination)
router.get('/', async function(req, res, next) {
    try {
        const page = parseInt(req.query.page) || 1;
        const limit = Math.min(parseInt(req.query.limit) || 20, 100); // Max 100 per page
        const offset = (page - 1) * limit;

        // Get clans with member count
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
            LIMIT ? OFFSET ?
        `;

        const clans = await executeQuery(query, [limit, offset]);

        // Get total count for pagination
        const countQuery = 'SELECT COUNT(*) as total FROM Clans';
        const countResult = await executeQuery(countQuery);
        const totalClans = countResult[0].total;

        res.json({
            message: 'Clans retrieved successfully',
            clans: clans.map(clan => ({
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag,
                description: clan.ClanDescription,
                leaderName: clan.LeaderName,
                memberCount: clan.MemberCount,
                creationDate: clan.CreationDate
            })),
            pagination: {
                currentPage: page,
                totalPages: Math.ceil(totalClans / limit),
                totalClans: totalClans,
                hasNext: page * limit < totalClans,
                hasPrev: page > 1
            }
        });

    } catch (error) {
        console.error('Get clans error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to retrieve clans'
        });
    }
});

// Get specific clan details (public route)
router.get('/:clanId', async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);

        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                error: 'Invalid clan ID',
                message: 'Clan ID must be a positive integer'
            });
        }

        // Get clan details with leader info
        const clanQuery = `
            SELECT 
                c.ClanID,
                c.ClanName,
                c.ClanTag,
                c.ClanDescription,
                c.CreationDate,
                u.Username as LeaderName,
                u.UserID as LeaderID
            FROM Clans c
            LEFT JOIN Users u ON c.ClanLeaderID = u.UserID
            WHERE c.ClanID = ?
        `;

        const clanResult = await executeQuery(clanQuery, [clanId]);

        if (clanResult.length === 0) {
            return res.status(404).json({
                error: 'Clan not found',
                message: 'The specified clan does not exist'
            });
        }

        const clan = clanResult[0];

        // Get clan members
        const membersQuery = `
            SELECT 
                cu.ClanRank,
                cu.JoinDate,
                u.Username,
                u.UserID
            FROM Clan_users cu
            JOIN Users u ON cu.UserID = u.UserID
            WHERE cu.ClanID = ?
            ORDER BY 
                CASE cu.ClanRank 
                    WHEN 'Leader' THEN 1 
                    WHEN 'Officer' THEN 2 
                    WHEN 'Member' THEN 3 
                    ELSE 4 
                END,
                cu.JoinDate ASC
        `;

        const members = await executeQuery(membersQuery, [clanId]);

        res.json({
            message: 'Clan details retrieved successfully',
            clan: {
                id: clan.ClanID,
                name: clan.ClanName,
                tag: clan.ClanTag,
                description: clan.ClanDescription,
                leaderName: clan.LeaderName,
                leaderId: clan.LeaderID,
                creationDate: clan.CreationDate,
                memberCount: members.length,
                members: members.map(member => ({
                    userId: member.UserID,
                    username: member.Username,
                    rank: member.ClanRank,
                    joinDate: member.JoinDate
                }))
            }
        });

    } catch (error) {
        console.error('Get clan details error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to retrieve clan details'
        });
    }
});

// Create a new clan (protected route)
router.post('/', authenticateToken, async function(req, res, next) {
    try {
        const { name, tag, description } = req.body;

        // Validate required fields
        if (!name || !tag) {
            return res.status(400).json({
                error: 'Missing required fields',
                message: 'Clan name and tag are required'
            });
        }

        // Validate clan name
        if (!validateClanName(name)) {
            return res.status(400).json({
                error: 'Invalid clan name',
                message: 'Clan name must be 3-255 characters long'
            });
        }

        // Validate clan tag
        if (!validateClanTag(tag)) {
            return res.status(400).json({
                error: 'Invalid clan tag',
                message: 'Clan tag must be 2-10 characters long and contain only letters and numbers'
            });
        }

        // Validate description if provided
        if (description && !validateClanDescription(description)) {
            return res.status(400).json({
                error: 'Invalid description',
                message: 'Clan description must be less than 500 characters'
            });
        }

        // Sanitize inputs
        const sanitizedName = sanitizeInput(name);
        const sanitizedTag = sanitizeInput(tag).toUpperCase();
        const sanitizedDescription = description ? sanitizeInput(description) : null;

        // Check if user is already in a clan
        const userClanQuery = 'SELECT ClanID FROM Clan_users WHERE UserID = ?';
        const userClanResult = await executeQuery(userClanQuery, [req.user.id]);

        if (userClanResult.length > 0) {
            return res.status(409).json({
                error: 'Already in clan',
                message: 'You must leave your current clan before creating a new one'
            });
        }

        // Check if clan name or tag already exists
        const existingClanQuery = 'SELECT ClanID FROM Clans WHERE ClanName = ? OR ClanTag = ?';
        const existingClan = await executeQuery(existingClanQuery, [sanitizedName, sanitizedTag]);

        if (existingClan.length > 0) {
            return res.status(409).json({
                error: 'Clan already exists',
                message: 'A clan with this name or tag already exists'
            });
        }

        // Create the clan
        const createClanQuery = `
            INSERT INTO Clans (ClanName, ClanTag, ClanDescription, ClanLeaderID) 
            VALUES (?, ?, ?, ?)
        `;
        const clanResult = await executeQuery(createClanQuery, [
            sanitizedName, 
            sanitizedTag, 
            sanitizedDescription, 
            req.user.id
        ]);

        const clanId = clanResult.insertId;

        // Add creator as clan leader
        const addLeaderQuery = `
            INSERT INTO Clan_users (UserID, ClanID, ClanRank) 
            VALUES (?, ?, 'Leader')
        `;
        await executeQuery(addLeaderQuery, [req.user.id, clanId]);

        res.status(201).json({
            message: 'Clan created successfully',
            clan: {
                id: clanId,
                name: sanitizedName,
                tag: sanitizedTag,
                description: sanitizedDescription,
                leaderId: req.user.id,
                leaderName: req.user.username
            }
        });

    } catch (error) {
        console.error('Create clan error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to create clan'
        });
    }
});

// Join a clan (protected route)
router.post('/:clanId/join', authenticateToken, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);

        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                error: 'Invalid clan ID',
                message: 'Clan ID must be a positive integer'
            });
        }

        // Check if clan exists
        const clanQuery = 'SELECT ClanID, ClanName FROM Clans WHERE ClanID = ?';
        const clanResult = await executeQuery(clanQuery, [clanId]);

        if (clanResult.length === 0) {
            return res.status(404).json({
                error: 'Clan not found',
                message: 'The specified clan does not exist'
            });
        }

        // Check if user is already in a clan
        const userClanQuery = 'SELECT ClanID FROM Clan_users WHERE UserID = ?';
        const userClanResult = await executeQuery(userClanQuery, [req.user.id]);

        if (userClanResult.length > 0) {
            return res.status(409).json({
                error: 'Already in clan',
                message: 'You are already a member of a clan'
            });
        }

        // Add user to clan
        const joinQuery = `
            INSERT INTO Clan_users (UserID, ClanID, ClanRank) 
            VALUES (?, ?, 'Member')
        `;
        await executeQuery(joinQuery, [req.user.id, clanId]);

        res.json({
            message: 'Successfully joined clan',
            clan: {
                id: clanId,
                name: clanResult[0].ClanName
            }
        });

    } catch (error) {
        console.error('Join clan error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to join clan'
        });
    }
});

// Leave clan (protected route)
router.post('/leave', authenticateToken, async function(req, res, next) {
    try {
        // Get user's current clan
        const userClanQuery = `
            SELECT cu.ClanID, cu.ClanRank, c.ClanName, c.ClanLeaderID
            FROM Clan_users cu
            JOIN Clans c ON cu.ClanID = c.ClanID
            WHERE cu.UserID = ?
        `;
        const userClanResult = await executeQuery(userClanQuery, [req.user.id]);

        if (userClanResult.length === 0) {
            return res.status(404).json({
                error: 'Not in clan',
                message: 'You are not a member of any clan'
            });
        }

        const userClan = userClanResult[0];

        // If user is the leader, check if there are other members
        if (userClan.ClanRank === 'Leader') {
            const memberCountQuery = 'SELECT COUNT(*) as count FROM Clan_users WHERE ClanID = ? AND UserID != ?';
            const memberCountResult = await executeQuery(memberCountQuery, [userClan.ClanID, req.user.id]);
            
            if (memberCountResult[0].count > 0) {
                return res.status(409).json({
                    error: 'Cannot leave clan',
                    message: 'As clan leader, you must transfer leadership or disband the clan before leaving'
                });
            }

            // If leader is the only member, delete the clan
            await executeQuery('DELETE FROM Clans WHERE ClanID = ?', [userClan.ClanID]);
        }

        // Remove user from clan
        const leaveQuery = 'DELETE FROM Clan_users WHERE UserID = ? AND ClanID = ?';
        await executeQuery(leaveQuery, [req.user.id, userClan.ClanID]);

        res.json({
            message: 'Successfully left clan',
            clan: {
                name: userClan.ClanName
            }
        });

    } catch (error) {
        console.error('Leave clan error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to leave clan'
        });
    }
});

// Get current user's clan info (protected route)
router.get('/my/clan', authenticateToken, async function(req, res, next) {
    try {
        // Get user's clan information
        const userClanQuery = `
            SELECT 
                c.ClanID,
                c.ClanName,
                c.ClanTag,
                c.ClanDescription,
                c.CreationDate,
                cu.ClanRank,
                cu.JoinDate,
                leader.Username as LeaderName
            FROM Clan_users cu
            JOIN Clans c ON cu.ClanID = c.ClanID
            JOIN Users leader ON c.ClanLeaderID = leader.UserID
            WHERE cu.UserID = ?
        `;
        
        const userClanResult = await executeQuery(userClanQuery, [req.user.id]);

        if (userClanResult.length === 0) {
            return res.status(404).json({
                error: 'Not in clan',
                message: 'You are not a member of any clan'
            });
        }

        const clanInfo = userClanResult[0];

        // Get member count
        const memberCountQuery = 'SELECT COUNT(*) as count FROM Clan_users WHERE ClanID = ?';
        const memberCountResult = await executeQuery(memberCountQuery, [clanInfo.ClanID]);

        res.json({
            message: 'Clan information retrieved successfully',
            clan: {
                id: clanInfo.ClanID,
                name: clanInfo.ClanName,
                tag: clanInfo.ClanTag,
                description: clanInfo.ClanDescription,
                leaderName: clanInfo.LeaderName,
                memberCount: memberCountResult[0].count,
                creationDate: clanInfo.CreationDate,
                userRank: clanInfo.ClanRank,
                userJoinDate: clanInfo.JoinDate
            }
        });

    } catch (error) {
        console.error('Get user clan error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to retrieve clan information'
        });
    }
});

// Update clan information (protected route - leaders/officers only)
router.put('/:clanId', authenticateToken, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);
        const { name, description } = req.body;

        if (!clanId || clanId <= 0) {
            return res.status(400).json({
                error: 'Invalid clan ID',
                message: 'Clan ID must be a positive integer'
            });
        }

        // Check if user has permission to update clan
        const permissionQuery = `
            SELECT cu.ClanRank, c.ClanName
            FROM Clan_users cu
            JOIN Clans c ON cu.ClanID = c.ClanID
            WHERE cu.UserID = ? AND cu.ClanID = ? AND cu.ClanRank IN ('Leader', 'Officer')
        `;
        
        const permissionResult = await executeQuery(permissionQuery, [req.user.id, clanId]);

        if (permissionResult.length === 0) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: 'Only clan leaders and officers can update clan information'
            });
        }

        const updates = [];
        const values = [];

        // Validate and add name update
        if (name !== undefined) {
            if (!validateClanName(name)) {
                return res.status(400).json({
                    error: 'Invalid clan name',
                    message: 'Clan name must be 3-255 characters long'
                });
            }

            const sanitizedName = sanitizeInput(name);
            
            // Check if name already exists (excluding current clan)
            const nameExistsQuery = 'SELECT ClanID FROM Clans WHERE ClanName = ? AND ClanID != ?';
            const nameExists = await executeQuery(nameExistsQuery, [sanitizedName, clanId]);
            
            if (nameExists.length > 0) {
                return res.status(409).json({
                    error: 'Name already exists',
                    message: 'A clan with this name already exists'
                });
            }

            updates.push('ClanName = ?');
            values.push(sanitizedName);
        }

        // Validate and add description update
        if (description !== undefined) {
            if (!validateClanDescription(description)) {
                return res.status(400).json({
                    error: 'Invalid description',
                    message: 'Clan description must be less than 500 characters'
                });
            }

            const sanitizedDescription = sanitizeInput(description) || null;
            updates.push('ClanDescription = ?');
            values.push(sanitizedDescription);
        }

        if (updates.length === 0) {
            return res.status(400).json({
                error: 'No updates provided',
                message: 'Please provide at least one field to update'
            });
        }

        // Update clan
        values.push(clanId);
        const updateQuery = `UPDATE Clans SET ${updates.join(', ')} WHERE ClanID = ?`;
        await executeQuery(updateQuery, values);

        res.json({
            message: 'Clan updated successfully'
        });

    } catch (error) {
        console.error('Update clan error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to update clan'
        });
    }
});

// Kick member from clan (protected route - leaders/officers only)
router.delete('/:clanId/members/:userId', authenticateToken, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.clanId);
        const targetUserId = parseInt(req.params.userId);

        if (!clanId || clanId <= 0 || !targetUserId || targetUserId <= 0) {
            return res.status(400).json({
                error: 'Invalid parameters',
                message: 'Clan ID and User ID must be positive integers'
            });
        }

        if (targetUserId === req.user.id) {
            return res.status(400).json({
                error: 'Cannot kick yourself',
                message: 'Use the leave clan endpoint to leave the clan'
            });
        }

        // Check permissions and get member info
        const permissionQuery = `
            SELECT 
                kicker.ClanRank as KickerRank,
                target.ClanRank as TargetRank,
                target_user.Username as TargetUsername
            FROM Clan_users kicker
            JOIN Clan_users target ON kicker.ClanID = target.ClanID
            JOIN Users target_user ON target.UserID = target_user.UserID
            WHERE kicker.UserID = ? AND kicker.ClanID = ? 
            AND target.UserID = ? AND kicker.ClanRank IN ('Leader', 'Officer')
        `;

        const permissionResult = await executeQuery(permissionQuery, [req.user.id, clanId, targetUserId]);

        if (permissionResult.length === 0) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: 'Only clan leaders and officers can kick members, and the target must be in the same clan'
            });
        }

        const { KickerRank, TargetRank, TargetUsername } = permissionResult[0];

        // Officers cannot kick other officers or leaders
        if (KickerRank === 'Officer' && (TargetRank === 'Officer' || TargetRank === 'Leader')) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: 'Officers cannot kick other officers or leaders'
            });
        }

        // Leaders cannot be kicked
        if (TargetRank === 'Leader') {
            return res.status(403).json({
                error: 'Cannot kick leader',
                message: 'Clan leaders cannot be kicked from the clan'
            });
        }

        // Remove user from clan
        const kickQuery = 'DELETE FROM Clan_users WHERE UserID = ? AND ClanID = ?';
        await executeQuery(kickQuery, [targetUserId, clanId]);

        res.json({
            message: 'Member kicked successfully',
            kickedUser: TargetUsername
        });

    } catch (error) {
        console.error('Kick member error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to kick member'
        });
    }
});

module.exports = router;
