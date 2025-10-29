var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');

// Get leaderboard - top users sorted by carrots (public route)
router.get('/', async function(req, res, next) {
    console.log(`[Leaderboard] GET /leaderboard - Query params:`, req.query);
    
    try {
        // Parse query parameters and ensure they are valid integers
        let limit = parseInt(req.query.limit) || 50;
        let offset = parseInt(req.query.offset) || 0;
        
        console.log(`[Leaderboard] Raw params - limit: ${req.query.limit}, offset: ${req.query.offset}`);
        
        // Validate and constrain parameters
        limit = Math.min(Math.max(1, limit), 100); // Between 1 and 100
        offset = Math.max(0, offset); // Non-negative
        
        console.log(`[Leaderboard] Validated params - limit: ${limit}, offset: ${offset}`);

        // Get users sorted by carrots descending, excluding banned users
        const leaderboardQuery = `
            SELECT UserID, Username, Carrots, G_Carrots, CreatedAt
            FROM Users 
            WHERE (IsBanned = 0 OR IsBanned IS NULL)
            ORDER BY Carrots DESC, G_Carrots DESC, CreatedAt ASC
            LIMIT ${limit} OFFSET ${offset}
        `;
        
        console.log(`[Leaderboard] Executing query:`, leaderboardQuery);
        const users = await executeQuery(leaderboardQuery, []);
        console.log(`[Leaderboard] Query returned ${users.length} users`);

        // Get total count for pagination info
        const countQuery = `
            SELECT COUNT(*) as total 
            FROM Users 
            WHERE IsBanned = 0 OR IsBanned IS NULL
        `;
        console.log(`[Leaderboard] Getting total count with query:`, countQuery);
        const countResult = await executeQuery(countQuery);
        const totalUsers = countResult[0].total;
        console.log(`[Leaderboard] Total users in database: ${totalUsers}`);

        // Format response
        const entries = users.map((user, index) => ({
            id: user.UserID,
            username: user.Username,
            carrots: user.Carrots,
            goldenCarrots: user.G_Carrots,
            rank: offset + index + 1
        }));

        console.log(`[Leaderboard] Formatted ${entries.length} entries for response`);
        console.log(`[Leaderboard] Sample entries:`, entries.slice(0, 3));

        const response = {
            message: 'Leaderboard retrieved successfully',
            entries: entries,
            pagination: {
                limit: limit,
                offset: offset,
                total: totalUsers,
                hasMore: (offset + limit) < totalUsers
            }
        };

        console.log(`[Leaderboard] Sending response with ${entries.length} entries, pagination:`, response.pagination);
        res.json(response);

    } catch (error) {
        console.error('[Leaderboard] Error retrieving leaderboard:', error);
        console.error('[Leaderboard] Error stack:', error.stack);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to retrieve leaderboard'
        });
    }
});

// Get user's rank and position in leaderboard (public route)
router.get('/rank/:userId', async function(req, res, next) {
    console.log(`[Leaderboard] GET /leaderboard/rank/${req.params.userId}`);
    
    try {
        const userId = parseInt(req.params.userId);
        console.log(`[Leaderboard] Parsed userId: ${userId}`);
        
        if (!userId || userId <= 0) {
            console.log(`[Leaderboard] Invalid userId provided: ${req.params.userId}`);
            return res.status(400).json({
                error: 'Invalid user ID',
                message: 'User ID must be a positive number'
            });
        }

        // Get user's current stats
        const userQuery = `
            SELECT UserID, Username, Carrots, G_Carrots
            FROM Users 
            WHERE UserID = ? AND (IsBanned = 0 OR IsBanned IS NULL)
        `;
        const userResult = await executeQuery(userQuery, [userId]);

        if (userResult.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User not found or is banned'
            });
        }

        const user = userResult[0];

        // Calculate user's rank
        const rankQuery = `
            SELECT COUNT(*) + 1 as rank
            FROM Users 
            WHERE (IsBanned = 0 OR IsBanned IS NULL)
            AND (
                Carrots > ? 
                OR (Carrots = ? AND G_Carrots > ?)
                OR (Carrots = ? AND G_Carrots = ? AND UserID < ?)
            )
        `;
        const rankResult = await executeQuery(rankQuery, [
            user.Carrots, 
            user.Carrots, user.G_Carrots,
            user.Carrots, user.G_Carrots, user.UserID
        ]);

        const rank = rankResult[0].rank;

        res.json({
            message: 'User rank retrieved successfully',
            user: {
                id: user.UserID,
                username: user.Username,
                carrots: user.Carrots,
                goldenCarrots: user.G_Carrots,
                rank: rank
            }
        });

    } catch (error) {
        console.error('User rank error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to retrieve user rank'
        });
    }
});

module.exports = router;