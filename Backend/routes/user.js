var bcrypt = require('bcrypt');
var express = require('express');
var router = express.Router();
const { executeQuery } = require('../lib/database');
const { authenticateToken } = require('../lib/auth');
const { validatePasswordChange } = require('../lib/validation');

// Get current user profile with game data (protected route)
router.get('/', authenticateToken, async function(req, res, next) {
    try {
        // Get user basic info and game data
        const userQuery = `
            SELECT UserID, Username, Email, Role, Carrots, HorseShoes, G_Carrots, CreatedAt
            FROM Users 
            WHERE UserID = ?
        `;
        const users = await executeQuery(userQuery, [req.user.id]);

        if (users.length === 0) {
            return res.status(404).json({ 
                error: 'User not found',
                message: 'User profile not found'
            });
        }

        const user = users[0];

        // Get user upgrades from the new table structure
        const upgradesQuery = `
            SELECT UpgradeName 
            FROM User_upgrades 
            WHERE UserID = ?
        `;
        const userUpgrades = await executeQuery(upgradesQuery, [req.user.id]);
        
        // Convert upgrades to object format for compatibility
        const upgradesObj = {};
        userUpgrades.forEach(upgrade => {
            upgradesObj[upgrade.UpgradeName] = true;
        });

        res.json({
            message: 'Profile retrieved successfully',
            user: {
                id: user.UserID,
                username: user.Username,
                email: user.Email,
                role: user.Role,
                createdAt: user.CreatedAt,
                gameData: {
                    carrots: user.Carrots,
                    horseShoes: user.HorseShoes,
                    goldenCarrots: user.G_Carrots,
                    upgrades: upgradesObj
                }
            }
        });

    } catch (error) {
        console.error('Profile error:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Sync currency data with server (protected route)
router.put('/sync-currency', authenticateToken, async function(req, res, next) {
    try {
        const { carrots, horseShoes, goldenCarrots } = req.body;

        // Validate currency data
        if (typeof carrots !== 'number' || typeof horseShoes !== 'number' || typeof goldenCarrots !== 'number') {
            return res.status(400).json({
                error: 'Invalid currency data',
                message: 'Currency values must be numbers'
            });
        }

        // Ensure non-negative values (basic anti-cheat)
        if (carrots < 0 || horseShoes < 0 || goldenCarrots < 0) {
            return res.status(400).json({
                error: 'Invalid currency values',
                message: 'Currency values cannot be negative'
            });
        }

        // Get current server values for comparison/logging
        const getCurrentQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
        const currentData = await executeQuery(getCurrentQuery, [req.user.id]);

        if (currentData.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User account not found'
            });
        }

        // Update currency in database
        const updateQuery = `
            UPDATE Users 
            SET Carrots = ?, HorseShoes = ?, G_Carrots = ?, UpdatedAt = CURRENT_TIMESTAMP 
            WHERE UserID = ?
        `;
        await executeQuery(updateQuery, [carrots, horseShoes, goldenCarrots, req.user.id]);

        // Log the sync for debugging (optional)
        console.log(`Currency sync for user ${req.user.id}: 
            Carrots: ${currentData[0].Carrots} -> ${carrots}
            HorseShoes: ${currentData[0].HorseShoes} -> ${horseShoes}
            Golden Carrots: ${currentData[0].G_Carrots} -> ${goldenCarrots}`);

        // Return updated currency data
        res.json({
            message: 'Currency synchronized successfully',
            currency: {
                carrots: carrots,
                horseShoes: horseShoes,
                goldenCarrots: goldenCarrots,
                lastSyncAt: new Date().toISOString()
            }
        });

    } catch (error) {
        console.error('Currency sync error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to sync currency data'
        });
    }
});

// Change password (protected route)
router.put('/change-password', authenticateToken, async function(req, res, next) {
    try {
        const { currentPassword, newPassword } = req.body;

        // Validate input using centralized validation
        const validation = validatePasswordChange({ currentPassword, newPassword });
        if (!validation.success) {
            return res.status(400).json({
                error: 'Invalid input',
                message: validation.errors[0] // Return first error
            });
        }

        const { currentPassword: sanitizedCurrentPassword, newPassword: sanitizedNewPassword } = validation.sanitizedData;

        // Get current user with password hash
        const getUserQuery = 'SELECT UserID, PasswordHash FROM Users WHERE UserID = ?';
        const users = await executeQuery(getUserQuery, [req.user.id]);

        if (users.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User account not found'
            });
        }

        const user = users[0];

        // Verify current password
        const isCurrentPasswordValid = await bcrypt.compare(sanitizedCurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid) {
            return res.status(401).json({
                error: 'Invalid current password',
                message: 'Current password is incorrect'
            });
        }

        // Check if new password is different from current password
        const isSamePassword = await bcrypt.compare(sanitizedNewPassword, user.PasswordHash);
        if (isSamePassword) {
            return res.status(400).json({
                error: 'Invalid new password',
                message: 'New password must be different from current password'
            });
        }

        // Hash new password
        const saltRounds = 12; // Consistent with registration
        const newPasswordHash = await bcrypt.hash(sanitizedNewPassword, saltRounds);

        // Update password in database
        const updateQuery = 'UPDATE Users SET PasswordHash = ?, UpdatedAt = CURRENT_TIMESTAMP WHERE UserID = ?';
        await executeQuery(updateQuery, [newPasswordHash, req.user.id]);

        // Return success response (don't include sensitive data)
        res.json({
            message: 'Password changed successfully'
        });

    } catch (error) {
        console.error('Change password error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to change password'
        });
    }


});

// Purchase upgrade (protected route)
router.post('/upgrade/:upgradeName', authenticateToken, async function(req, res, next) {
    try {
        const upgradeName = req.params.upgradeName;
        
        // Validate upgrade name (you can add more validation as needed)
        if (!upgradeName || typeof upgradeName !== 'string') {
            return res.status(400).json({
                error: 'Invalid upgrade name',
                message: 'Upgrade name is required'
            });
        }

        // Check if user already has this upgrade
        const existingUpgradeQuery = `
            SELECT UserUpgradeID 
            FROM User_upgrades 
            WHERE UserID = ? AND UpgradeName = ?
        `;
        const existingUpgrade = await executeQuery(existingUpgradeQuery, [req.user.id, upgradeName]);

        if (existingUpgrade.length > 0) {
            return res.status(400).json({
                error: 'Upgrade already owned',
                message: 'User already has this upgrade'
            });
        }

        // Add the upgrade to the user
        const purchaseUpgradeQuery = `
            INSERT INTO User_upgrades (UserID, UpgradeName, BuyDate) 
            VALUES (?, ?, CURRENT_TIMESTAMP)
        `;
        await executeQuery(purchaseUpgradeQuery, [req.user.id, upgradeName]);

        res.json({
            message: 'Upgrade purchased successfully',
            upgrade: {
                name: upgradeName,
                purchasedAt: new Date().toISOString()
            }
        });

    } catch (error) {
        console.error('Purchase upgrade error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to purchase upgrade'
        });
    }
});

// Get user upgrades (protected route)
router.get('/upgrades', authenticateToken, async function(req, res, next) {
    try {
        const upgradesQuery = `
            SELECT UpgradeName, BuyDate 
            FROM User_upgrades 
            WHERE UserID = ?
            ORDER BY BuyDate ASC
        `;
        const upgrades = await executeQuery(upgradesQuery, [req.user.id]);

        res.json({
            message: 'Upgrades retrieved successfully',
            upgrades: upgrades.map(upgrade => ({
                name: upgrade.UpgradeName,
                purchasedAt: upgrade.BuyDate
            }))
        });

    } catch (error) {
        console.error('Get upgrades error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to retrieve upgrades'
        });
    }
});

// Purchase/Update building (protected route)
router.post('/building/:buildingName', authenticateToken, async function(req, res, next) {
    try {
        const buildingName = req.params.buildingName;
        const { count } = req.body;
        
        // Validate inputs
        if (!buildingName || typeof buildingName !== 'string') {
            return res.status(400).json({
                error: 'Invalid building name',
                message: 'Building name is required'
            });
        }
        
        if (typeof count !== 'number' || count < 0) {
            return res.status(400).json({
                error: 'Invalid count',
                message: 'Count must be a non-negative number'
            });
        }

        // Check if user already has this building
        const existingBuildingQuery = `
            SELECT UserBuildingID, Count 
            FROM User_buildings 
            WHERE UserID = ? AND BuildingName = ?
        `;
        const existingBuilding = await executeQuery(existingBuildingQuery, [req.user.id, buildingName]);

        if (existingBuilding.length > 0) {
            // Update existing building count
            const updateBuildingQuery = `
                UPDATE User_buildings 
                SET Count = ? 
                WHERE UserID = ? AND BuildingName = ?
            `;
            await executeQuery(updateBuildingQuery, [count, req.user.id, buildingName]);
        } else {
            // Add new building
            const addBuildingQuery = `
                INSERT INTO User_buildings (UserID, BuildingName, Count, BuyDate) 
                VALUES (?, ?, ?, CURRENT_TIMESTAMP)
            `;
            await executeQuery(addBuildingQuery, [req.user.id, buildingName, count]);
        }

        res.json({
            message: 'Building updated successfully',
            building: {
                name: buildingName,
                count: count
            }
        });

    } catch (error) {
        console.error('Update building error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to update building'
        });
    }
});

// Get user buildings (protected route)
router.get('/buildings', authenticateToken, async function(req, res, next) {
    try {
        const buildingsQuery = `
            SELECT BuildingName, Count, BuyDate 
            FROM User_buildings 
            WHERE UserID = ?
            ORDER BY BuyDate ASC
        `;
        const buildings = await executeQuery(buildingsQuery, [req.user.id]);

        res.json({
            message: 'Buildings retrieved successfully',
            buildings: buildings.map(building => ({
                name: building.BuildingName,
                count: building.Count,
                firstPurchased: building.BuyDate
            }))
        });

    } catch (error) {
        console.error('Get buildings error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to retrieve buildings'
        });
    }
});

// Unlock achievement (protected route)
router.post('/achievement/:achievementName', authenticateToken, async function(req, res, next) {
    try {
        const achievementName = req.params.achievementName;
        
        // Validate achievement name
        if (!achievementName || typeof achievementName !== 'string') {
            return res.status(400).json({
                error: 'Invalid achievement name',
                message: 'Achievement name is required'
            });
        }

        // Check if user already has this achievement
        const existingAchievementQuery = `
            SELECT UserAchievementID 
            FROM User_achievements 
            WHERE UserID = ? AND AchievementName = ?
        `;
        const existingAchievement = await executeQuery(existingAchievementQuery, [req.user.id, achievementName]);

        if (existingAchievement.length > 0) {
            return res.status(400).json({
                error: 'Achievement already unlocked',
                message: 'User already has this achievement'
            });
        }

        // Add the achievement to the user
        const unlockAchievementQuery = `
            INSERT INTO User_achievements (UserID, AchievementName, EarnDate) 
            VALUES (?, ?, CURRENT_TIMESTAMP)
        `;
        await executeQuery(unlockAchievementQuery, [req.user.id, achievementName]);

        res.json({
            message: 'Achievement unlocked successfully',
            achievement: {
                name: achievementName,
                unlockedAt: new Date().toISOString()
            }
        });

    } catch (error) {
        console.error('Unlock achievement error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to unlock achievement'
        });
    }
});

// Get user achievements (protected route)
router.get('/achievements', authenticateToken, async function(req, res, next) {
    try {
        const achievementsQuery = `
            SELECT AchievementName, EarnDate 
            FROM User_achievements 
            WHERE UserID = ?
            ORDER BY EarnDate ASC
        `;
        const achievements = await executeQuery(achievementsQuery, [req.user.id]);

        res.json({
            message: 'Achievements retrieved successfully',
            achievements: achievements.map(achievement => ({
                name: achievement.AchievementName,
                unlockedAt: achievement.EarnDate
            }))
        });

    } catch (error) {
        console.error('Get achievements error:', error);
        res.status(500).json({
            error: 'Internal server error',
            message: 'Failed to retrieve achievements'
        });
    }
});

module.exports = router;
