var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { sanitizeInput } = require('../lib/validation');

// Middleware: require admin role
async function isAdmin(req, res, next) {
    try {
        if (!req.user || !req.user.id) {
            return res.status(401).json({ error: 'Authentication required' });
        }

        const userId = req.user.id;
        const roleQuery = 'SELECT Role FROM Users WHERE UserID = ?';
        const rows = await executeQuery(roleQuery, [userId]);

        if (rows.length === 0) {
            return res.status(404).json({ error: 'User not found' });
        }

        const role = rows[0].Role;
        if (role !== 'Admin') {
            return res.status(403).json({ error: 'Admin privileges required' });
        }

        next();
    } catch (err) {
        console.error('isAdmin middleware error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
}

// Helper: ensure IsBanned column exists (safe to call)
async function ensureBannedColumn() {
    try {
        // Try selecting the column
        await executeQuery('SELECT IsBanned FROM Users LIMIT 1');
        return true;
    } catch (err) {
        // Do NOT alter schema at runtime here. Require an explicit migration.
        const message = 'IsBanned column missing in Users table. Please add it via a database migration before using ban/unban endpoints.';
        console.error(message, err);
        throw new Error(message);
    }
}

// GET /admin/stats - general statistics
router.get('/stats', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        // Accounts total and admins count
        const accountsQuery = `
            SELECT 
                (SELECT COUNT(*) FROM Users) as totalUsers,
                (SELECT COUNT(*) FROM Users WHERE Role = 'Admin') as totalAdmins,
                (SELECT COUNT(*) FROM Users WHERE UpdatedAt >= DATE_SUB(NOW(), INTERVAL 24 HOUR)) as activeLast24h
        `;
        const accounts = await executeQuery(accountsQuery);

        // Clans stats
        const clansQuery = `
            SELECT 
                (SELECT COUNT(*) FROM Clans) as totalClans,
                (SELECT COUNT(*) FROM Clan_users) as totalClanMemberships
        `;
        const clans = await executeQuery(clansQuery);

        // Top clans by member count
        const topClansQuery = `
            SELECT c.ClanID, c.ClanName, c.ClanTag, COUNT(cu.UserID) as memberCount
            FROM Clans c
            LEFT JOIN Clan_users cu ON c.ClanID = cu.ClanID
            GROUP BY c.ClanID, c.ClanName, c.ClanTag
            ORDER BY memberCount DESC
            LIMIT 10
        `;
        const topClans = await executeQuery(topClansQuery);

        res.json({
            message: 'Admin statistics retrieved',
            accounts: accounts[0] || {},
            clans: clans[0] || {},
            topClans: topClans.map(c => ({ id: c.ClanID, name: c.ClanName, tag: c.ClanTag, memberCount: c.memberCount }))
        });

    } catch (err) {
        console.error('Admin stats error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// GET /admin/users - list all users with pagination
router.get('/users', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const page = Math.max(1, parseInt(req.query.page) || 1);
        const limit = Math.min(1000, Math.max(1, parseInt(req.query.limit) || 50)); // Cap at 1000 users
        const offset = (page - 1) * limit;
        
        console.log(`Admin users request: page=${page}, limit=${limit}, offset=${offset}`);

        // We'll get the total count from the full query result

        // Get users with pagination
        // Alternative approach: get all users and slice in JavaScript (for smaller datasets)
        // This avoids MySQL parameter binding issues with LIMIT/OFFSET
        let users;
        let totalUsers = 0;
        
        try {
            // Try with IsBanned column first
            const allUsersQuery = `
                SELECT UserID, Username, Email, Role, Carrots, HorseShoes, G_Carrots, 
                       CreatedAt, UpdatedAt, COALESCE(IsBanned, 0) as IsBanned
                FROM Users 
                ORDER BY CreatedAt DESC
            `;
            const allUsers = await executeQuery(allUsersQuery);
            totalUsers = allUsers.length;
            
            // Apply pagination in JavaScript
            const startIndex = offset;
            const endIndex = offset + limit;
            users = allUsers.slice(startIndex, endIndex);
            
        } catch (err) {
            if (err.message && err.message.includes('IsBanned')) {
                // Fallback without IsBanned column
                console.log('IsBanned column not found, using fallback query');
                const fallbackQuery = `
                    SELECT UserID, Username, Email, Role, Carrots, HorseShoes, G_Carrots, 
                           CreatedAt, UpdatedAt, 0 as IsBanned
                    FROM Users 
                    ORDER BY CreatedAt DESC
                `;
                const allUsers = await executeQuery(fallbackQuery);
                totalUsers = allUsers.length;
                
                // Apply pagination in JavaScript
                const startIndex = offset;
                const endIndex = offset + limit;
                users = allUsers.slice(startIndex, endIndex);
            } else {
                throw err;
            }
        }

        res.json({
            message: 'Users retrieved successfully',
            users: users.map(user => ({
                id: user.UserID,
                username: user.Username,
                email: user.Email,
                role: user.Role,
                carrots: user.Carrots,
                horseShoes: user.HorseShoes,
                goldenCarrots: user.G_Carrots,
                createdAt: user.CreatedAt,
                updatedAt: user.UpdatedAt,
                isBanned: Boolean(user.IsBanned)
            })),
            pagination: {
                currentPage: page,
                totalPages: Math.ceil(totalUsers / limit),
                totalUsers: totalUsers,
                usersPerPage: limit
            }
        });

    } catch (err) {
        console.error('Admin users list error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/user/:id/promote - make user an admin
router.post('/user/:id/promote', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const targetId = parseInt(req.params.id);
        if (!targetId || targetId <= 0) return res.status(400).json({ error: 'Invalid user id' });

        await executeQuery("UPDATE Users SET Role = 'Admin' WHERE UserID = ?", [targetId]);
        res.json({ message: 'User promoted to Admin', userId: targetId });
    } catch (err) {
        console.error('Promote user error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/user/:id/demote - remove admin role
router.post('/user/:id/demote', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const targetId = parseInt(req.params.id);
        if (!targetId || targetId <= 0) return res.status(400).json({ error: 'Invalid user id' });

        await executeQuery("UPDATE Users SET Role = 'User' WHERE UserID = ?", [targetId]);
        res.json({ message: 'User demoted to User', userId: targetId });
    } catch (err) {
        console.error('Demote user error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// DELETE /admin/user/:id - delete user and cleanup
router.delete('/user/:id', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const targetId = parseInt(req.params.id);
        if (!targetId || targetId <= 0) return res.status(400).json({ error: 'Invalid user id' });

        // Remove all foreign key references before deleting user
        await executeQuery('DELETE FROM Clan_users WHERE UserID = ?', [targetId]);
        await executeQuery('DELETE FROM User_upgrades WHERE UserID = ?', [targetId]);
        await executeQuery('DELETE FROM User_achievements WHERE UserID = ?', [targetId]);
        await executeQuery('DELETE FROM Users WHERE UserID = ?', [targetId]);

        res.json({ message: 'User deleted', userId: targetId });
    } catch (err) {
        console.error('Delete user error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/user/:id/ban - ban user (adds IsBanned column if needed)
router.post('/user/:id/ban', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const targetId = parseInt(req.params.id);
        if (!targetId || targetId <= 0) return res.status(400).json({ error: 'Invalid user id' });

        await ensureBannedColumn();
        await executeQuery('UPDATE Users SET IsBanned = 1 WHERE UserID = ?', [targetId]);
        res.json({ message: 'User banned', userId: targetId });
    } catch (err) {
        console.error('Ban user error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/user/:id/unban - unban user
router.post('/user/:id/unban', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const targetId = parseInt(req.params.id);
        if (!targetId || targetId <= 0) return res.status(400).json({ error: 'Invalid user id' });

        await ensureBannedColumn();
        await executeQuery('UPDATE Users SET IsBanned = 0 WHERE UserID = ?', [targetId]);
        res.json({ message: 'User unbanned', userId: targetId });
    } catch (err) {
        console.error('Unban user error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/clan/:id/disband - delete a clan and its memberships
router.post('/clan/:id/disband', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.id);
        if (!clanId || clanId <= 0) return res.status(400).json({ error: 'Invalid clan id' });

        await executeQuery('DELETE FROM Clan_users WHERE ClanID = ?', [clanId]);
        await executeQuery('DELETE FROM Clans WHERE ClanID = ?', [clanId]);

        res.json({ message: 'Clan disbanded', clanId });
    } catch (err) {
        console.error('Disband clan error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/clan/:id/remove-member - remove a member from a clan (body: userId)
router.post('/clan/:id/remove-member', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const clanId = parseInt(req.params.id);
        const userId = parseInt(req.body.userId);
        if (!clanId || clanId <= 0 || !userId || userId <= 0) return res.status(400).json({ error: 'Invalid ids' });

        await executeQuery('DELETE FROM Clan_users WHERE ClanID = ? AND UserID = ?', [clanId, userId]);
        res.json({ message: 'Member removed from clan', clanId, userId });
    } catch (err) {
        console.error('Remove clan member error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// POST /admin/user/:id/set-currency - set user's currency values (body: carrots, horseShoes, goldenCarrots)
router.post('/user/:id/set-currency', authenticateToken, isAdmin, async function(req, res, next) {
    try {
        const targetId = parseInt(req.params.id);
        if (!targetId || targetId <= 0) return res.status(400).json({ error: 'Invalid user id' });

        const carrots = req.body.carrots !== undefined ? parseInt(req.body.carrots) : undefined;
        const horseShoes = req.body.horseShoes !== undefined ? parseInt(req.body.horseShoes) : undefined;
        const goldenCarrots = req.body.goldenCarrots !== undefined ? parseInt(req.body.goldenCarrots) : undefined;

        const updates = [];
        const values = [];
        if (carrots !== undefined) { updates.push('Carrots = ?'); values.push(carrots); }
        if (horseShoes !== undefined) { updates.push('HorseShoes = ?'); values.push(horseShoes); }
        if (goldenCarrots !== undefined) { updates.push('G_Carrots = ?'); values.push(goldenCarrots); }

        if (updates.length === 0) return res.status(400).json({ error: 'No currency values provided' });

        values.push(targetId);
        const updateQuery = `UPDATE Users SET ${updates.join(', ')} WHERE UserID = ?`;
        await executeQuery(updateQuery, values);

        const updated = await executeQuery('SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?', [targetId]);
        res.json({ message: 'Currency updated', userId: targetId, currency: updated[0] });
    } catch (err) {
        console.error('Set currency error:', err);
        res.status(500).json({ error: 'Internal server error' });
    }
});

module.exports = router;
