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
            SELECT u.UserID, u.Username, u.Email, u.Carrots, u.HorseShoes, u.G_Carrots, u.CreatedAt,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const users = await executeQuery(userQuery, [req.user.id]);

        if (users.length === 0) {
            return res.status(404).json({ 
                error: 'User not found',
                message: 'User profile not found'
            });
        }

        const user = users[0];

        // If no upgrades record exists, create one with default values
        if (user.Upgrade1 === null) {
            const createUpgradesQuery = 'INSERT INTO User_upgrades (UserID) VALUES (?)';
            await executeQuery(createUpgradesQuery, [req.user.id]);
            
            // Set default upgrade values
            user.Upgrade1 = false;
            user.Upgrade2 = false;
            user.Upgrade3 = false;
        }

        res.json({
            message: 'Profile retrieved successfully',
            user: {
                id: user.UserID,
                username: user.Username,
                email: user.Email,
                createdAt: user.CreatedAt,
                gameData: {
                    carrots: user.Carrots,
                    horseShoes: user.HorseShoes,
                    goldenCarrots: user.G_Carrots,
                    upgrades: {
                        upgrade1: Boolean(user.Upgrade1),
                        upgrade2: Boolean(user.Upgrade2),
                        upgrade3: Boolean(user.Upgrade3)
                    }
                }
            }
        });

    } catch (error) {
        console.error('Profile error:', error);
        res.status(500).json({ error: 'Internal server error' });
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

// Get user upgrades (protected route)
router.get('/upgrades', authenticateToken, async function(req, res, next) {
    try {
        const upgradesQuery = `
            SELECT uu.Upgrade1, uu.Upgrade2, uu.Upgrade3,
                   u.Carrots, u.HorseShoes, u.G_Carrots
            FROM User_upgrades uu
            JOIN Users u ON uu.UserID = u.UserID
            WHERE uu.UserID = ?
        `;
        const upgradesResult = await executeQuery(upgradesQuery, [req.user.id]);

        if (upgradesResult.length === 0) {
            // Create upgrades record if it doesn't exist
            const createQuery = 'INSERT INTO User_upgrades (UserID) VALUES (?)';
            await executeQuery(createQuery, [req.user.id]);

            // Get user's currency for response
            const currencyQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
            const currencyResult = await executeQuery(currencyQuery, [req.user.id]);
            const currency = currencyResult[0] || { Carrots: 0, HorseShoes: 0, G_Carrots: 0 };

            return res.json({
                message: 'Upgrades retrieved successfully',
                upgrades: {
                    upgrade1: false,
                    upgrade2: false,
                    upgrade3: false
                },
                currency: {
                    carrots: currency.Carrots,
                    horseShoes: currency.HorseShoes,
                    goldenCarrots: currency.G_Carrots
                }
            });
        }

        const upgrades = upgradesResult[0];
        res.json({
            message: 'Upgrades retrieved successfully',
            upgrades: {
                upgrade1: Boolean(upgrades.Upgrade1),
                upgrade2: Boolean(upgrades.Upgrade2),
                upgrade3: Boolean(upgrades.Upgrade3)
            },
            currency: {
                carrots: upgrades.Carrots,
                horseShoes: upgrades.HorseShoes,
                goldenCarrots: upgrades.G_Carrots
            }
        });

    } catch (error) {
        console.error('Get upgrades error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to retrieve upgrades'
        });
    }
});

// Purchase/unlock upgrade (protected route)
router.post('/upgrades/:upgradeId/purchase', authenticateToken, async function(req, res, next) {
    try {
        const upgradeId = parseInt(req.params.upgradeId);

        // Validate upgrade ID
        if (!upgradeId || upgradeId < 1 || upgradeId > 3) {
            return res.status(400).json({
                error: 'Invalid upgrade ID',
                message: 'Upgrade ID must be 1, 2, or 3'
            });
        }

        // Define upgrade costs and requirements
        const upgradeCosts = {
            1: { carrots: 100, horseShoes: 0, goldenCarrots: 0, name: 'Basic Upgrade' },
            2: { carrots: 500, horseShoes: 10, goldenCarrots: 0, name: 'Advanced Upgrade', requires: [1] },
            3: { carrots: 1000, horseShoes: 50, goldenCarrots: 5, name: 'Premium Upgrade', requires: [1, 2] }
        };

        const upgrade = upgradeCosts[upgradeId];
        const upgradeColumn = `Upgrade${upgradeId}`;

        // Get current user data and upgrades
        const userDataQuery = `
            SELECT u.Carrots, u.HorseShoes, u.G_Carrots,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const userDataResult = await executeQuery(userDataQuery, [req.user.id]);

        if (userDataResult.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User data not found'
            });
        }

        const userData = userDataResult[0];

        // Create upgrades record if it doesn't exist
        if (userData.Upgrade1 === null) {
            const createQuery = 'INSERT INTO User_upgrades (UserID) VALUES (?)';
            await executeQuery(createQuery, [req.user.id]);
            userData.Upgrade1 = false;
            userData.Upgrade2 = false;
            userData.Upgrade3 = false;
        }

        // Check if upgrade is already purchased
        if (userData[upgradeColumn]) {
            return res.status(409).json({
                error: 'Already purchased',
                message: 'This upgrade has already been purchased'
            });
        }

        // Check prerequisites
        if (upgrade.requires) {
            for (const requiredUpgrade of upgrade.requires) {
                if (!userData[`Upgrade${requiredUpgrade}`]) {
                    return res.status(400).json({
                        error: 'Prerequisites not met',
                        message: `You must purchase Upgrade ${requiredUpgrade} first`
                    });
                }
            }
        }

        // Check if user has enough currency
        if (userData.Carrots < upgrade.carrots ||
            userData.HorseShoes < upgrade.horseShoes ||
            userData.G_Carrots < upgrade.goldenCarrots) {
            return res.status(400).json({
                error: 'Insufficient funds',
                message: `Not enough currency. Required: ${upgrade.carrots} carrots, ${upgrade.horseShoes} horse shoes, ${upgrade.goldenCarrots} golden carrots`,
                required: {
                    carrots: upgrade.carrots,
                    horseShoes: upgrade.horseShoes,
                    goldenCarrots: upgrade.goldenCarrots
                },
                current: {
                    carrots: userData.Carrots,
                    horseShoes: userData.HorseShoes,
                    goldenCarrots: userData.G_Carrots
                }
            });
        }

        // Start transaction - deduct currency and unlock upgrade
        const updateUserQuery = `
            UPDATE Users 
            SET Carrots = Carrots - ?, 
                HorseShoes = HorseShoes - ?, 
                G_Carrots = G_Carrots - ?
            WHERE UserID = ?
        `;
        await executeQuery(updateUserQuery, [upgrade.carrots, upgrade.horseShoes, upgrade.goldenCarrots, req.user.id]);

        const updateUpgradeQuery = `UPDATE User_upgrades SET ${upgradeColumn} = TRUE WHERE UserID = ?`;
        await executeQuery(updateUpgradeQuery, [req.user.id]);

        // Get updated currency
        const newCurrencyQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
        const newCurrencyResult = await executeQuery(newCurrencyQuery, [req.user.id]);
        const newCurrency = newCurrencyResult[0];

        res.json({
            message: `${upgrade.name} purchased successfully!`,
            upgrade: {
                id: upgradeId,
                name: upgrade.name,
                cost: {
                    carrots: upgrade.carrots,
                    horseShoes: upgrade.horseShoes,
                    goldenCarrots: upgrade.goldenCarrots
                }
            },
            newCurrency: {
                carrots: newCurrency.Carrots,
                horseShoes: newCurrency.HorseShoes,
                goldenCarrots: newCurrency.G_Carrots
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

// Sync game session with offline earnings calculation (protected route)
router.post('/sync', authenticateToken, async function(req, res, next) {
    try {
        const { 
            sessionData, 
            clickCount = 0, 
            sessionDuration = 0, 
            isReturningPlayer = false
        } = req.body;

        // Get current server-side data including last update time
        const currentDataQuery = `
            SELECT u.Carrots, u.HorseShoes, u.G_Carrots, u.UpdatedAt,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const currentDataResult = await executeQuery(currentDataQuery, [req.user.id]);
        
        if (currentDataResult.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User data not found'
            });
        }

        const serverData = currentDataResult[0];
        const lastUpdateTime = new Date(serverData.UpdatedAt);
        const currentTime = new Date();
        
        // Calculate offline time in seconds
        const offlineTimeSeconds = Math.floor((currentTime - lastUpdateTime) / 1000);
        
        // Define base earning rates per second (idle earnings)
        const baseIdleRates = {
            carrots: 0.5,  // Base idle earning rate
            horseShoes: 0,  // Only available with Upgrade2
            goldenCarrots: 0  // Only available with Upgrade3
        };

        // Apply upgrade effects to idle earnings
        if (serverData.Upgrade1) {
            baseIdleRates.carrots *= 1.5;  // 50% boost to carrot production
        }
        if (serverData.Upgrade2) {
            baseIdleRates.horseShoes = 0.05;  // Enable horse shoe generation
        }
        if (serverData.Upgrade3) {
            baseIdleRates.goldenCarrots = 0.005;  // Enable golden carrot generation
        }

        // Calculate offline earnings
        let offlineEarnings = { carrots: 0, horseShoes: 0, goldenCarrots: 0 };
        let offlineMultiplier = 1.0;
        let maxOfflineHours = 24; // Maximum offline earnings cap
        
        if (isReturningPlayer && offlineTimeSeconds > 300) { // Only if offline for more than 5 minutes
            const offlineHours = Math.min(offlineTimeSeconds / 3600, maxOfflineHours);
            
            // Apply diminishing returns for longer offline periods
            if (offlineHours > 2) {
                offlineMultiplier = 0.7; // 70% efficiency after 2 hours
            } else if (offlineHours > 8) {
                offlineMultiplier = 0.5; // 50% efficiency after 8 hours
            } else if (offlineHours > 16) {
                offlineMultiplier = 0.3; // 30% efficiency after 16 hours
            }

            offlineEarnings = {
                carrots: Math.floor(offlineTimeSeconds * baseIdleRates.carrots * offlineMultiplier),
                horseShoes: Math.floor(offlineTimeSeconds * baseIdleRates.horseShoes * offlineMultiplier),
                goldenCarrots: Math.floor(offlineTimeSeconds * baseIdleRates.goldenCarrots * offlineMultiplier)
            };
        }

        // Calculate active session earnings validation
        const activeEarningRates = {
            carrots: 2.0,  // Active play earns more
            horseShoes: serverData.Upgrade2 ? 0.2 : 0,
            goldenCarrots: serverData.Upgrade3 ? 0.02 : 0
        };

        // Apply upgrade multipliers to active earnings
        const activeMultipliers = {
            carrots: serverData.Upgrade1 ? 1.5 : 1.0,
            horseShoes: 1.0,
            goldenCarrots: 1.0
        };

        // Calculate maximum possible active earnings for validation
        const maxActiveEarnings = {
            carrots: Math.floor(sessionDuration * activeEarningRates.carrots * activeMultipliers.carrots),
            horseShoes: Math.floor(sessionDuration * activeEarningRates.horseShoes * activeMultipliers.horseShoes),
            goldenCarrots: Math.floor(sessionDuration * activeEarningRates.goldenCarrots * activeMultipliers.goldenCarrots)
        };

        // Add click earnings (base click value with upgrade multiplier)
        const baseClickValue = 1;
        const clickEarnings = Math.floor(clickCount * baseClickValue * activeMultipliers.carrots);
        maxActiveEarnings.carrots += clickEarnings;

        // Validate client active session data against server calculations
        const clientEarnings = sessionData || {};
        
        // Check for suspicious activity (client claiming more than possible)
        const suspiciousActivity = {
            carrots: (clientEarnings.carrots || 0) > maxActiveEarnings.carrots,
            horseShoes: (clientEarnings.horseShoes || 0) > maxActiveEarnings.horseShoes,
            goldenCarrots: (clientEarnings.goldenCarrots || 0) > maxActiveEarnings.goldenCarrots
        };

        // Calculate how much the client tried to over-earn
        const attemptedOverEarning = {
            carrots: Math.max(0, (clientEarnings.carrots || 0) - maxActiveEarnings.carrots),
            horseShoes: Math.max(0, (clientEarnings.horseShoes || 0) - maxActiveEarnings.horseShoes),
            goldenCarrots: Math.max(0, (clientEarnings.goldenCarrots || 0) - maxActiveEarnings.goldenCarrots)
        };

        // Check for extreme cheating attempts (more than 10x possible earnings)
        const extremeCheating = 
            attemptedOverEarning.carrots > maxActiveEarnings.carrots * 10 ||
            attemptedOverEarning.horseShoes > maxActiveEarnings.horseShoes * 10 ||
            attemptedOverEarning.goldenCarrots > maxActiveEarnings.goldenCarrots * 10;

        // Log suspicious activity
        if (suspiciousActivity.carrots || suspiciousActivity.horseShoes || suspiciousActivity.goldenCarrots) {
            console.warn(`🚨 CHEAT DETECTION - User ${req.user.id} (${req.user.username}):`, {
                attempted: clientEarnings,
                maximum: maxActiveEarnings,
                overEarning: attemptedOverEarning,
                sessionDuration: sessionDuration,
                clickCount: clickCount,
                timestamp: new Date().toISOString(),
                extremeCheating: extremeCheating
            });

            // For extreme cheating, you might want to flag the account
            if (extremeCheating) {
                console.error(`🔥 EXTREME CHEAT ATTEMPT - User ${req.user.id} attempted to earn ${JSON.stringify(attemptedOverEarning)} over maximum!`);
                // TODO: Consider implementing account flagging/temporary restrictions
            }
        }

        // Validate and cap earnings at server-calculated maximums
        const validatedActiveEarnings = {
            carrots: Math.min(clientEarnings.carrots || 0, maxActiveEarnings.carrots),
            horseShoes: Math.min(clientEarnings.horseShoes || 0, maxActiveEarnings.horseShoes),
            goldenCarrots: Math.min(clientEarnings.goldenCarrots || 0, maxActiveEarnings.goldenCarrots)
        };

        // Additional validation: Check for impossible click rates
        const maxHumanClickRate = 10; // clicks per second (generous for mobile)
        const reportedClickRate = sessionDuration > 0 ? clickCount / sessionDuration : 0;
        
        if (reportedClickRate > maxHumanClickRate) {
            console.warn(`🤖 IMPOSSIBLE CLICK RATE - User ${req.user.id}: ${reportedClickRate.toFixed(2)} clicks/sec (max human: ${maxHumanClickRate})`);
            
            // Reduce click earnings for impossible click rates
            const adjustedClickCount = Math.floor(sessionDuration * maxHumanClickRate);
            const adjustedClickEarnings = Math.floor(adjustedClickCount * baseClickValue * activeMultipliers.carrots);
            
            // Recalculate max earnings with adjusted clicks
            maxActiveEarnings.carrots = Math.floor(sessionDuration * activeEarningRates.carrots * activeMultipliers.carrots) + adjustedClickEarnings;
            validatedActiveEarnings.carrots = Math.min(clientEarnings.carrots || 0, maxActiveEarnings.carrots);
        }

        // Combine offline and active earnings
        const totalEarnings = {
            carrots: offlineEarnings.carrots + validatedActiveEarnings.carrots,
            horseShoes: offlineEarnings.horseShoes + validatedActiveEarnings.horseShoes,
            goldenCarrots: offlineEarnings.goldenCarrots + validatedActiveEarnings.goldenCarrots
        };

        // Update currency in database
        const updateQuery = `
            UPDATE Users 
            SET Carrots = Carrots + ?, 
                HorseShoes = HorseShoes + ?, 
                G_Carrots = G_Carrots + ?,
                UpdatedAt = CURRENT_TIMESTAMP
            WHERE UserID = ?
        `;
        
        await executeQuery(updateQuery, [
            totalEarnings.carrots,
            totalEarnings.horseShoes,
            totalEarnings.goldenCarrots,
            req.user.id
        ]);

        // Get updated currency totals
        const updatedDataQuery = 'SELECT Carrots, HorseShoes, G_Carrots, UpdatedAt FROM Users WHERE UserID = ?';
        const updatedDataResult = await executeQuery(updatedDataQuery, [req.user.id]);
        const updatedData = updatedDataResult[0];

        // Prepare response with detailed breakdown
        const response = {
            message: 'Game session synced successfully',
            syncResult: {
                totalEarnings: totalEarnings,
                currentTotals: {
                    carrots: updatedData.Carrots,
                    horseShoes: updatedData.HorseShoes,
                    goldenCarrots: updatedData.G_Carrots
                },
                serverTime: new Date().toISOString(),
                breakdown: {
                    offlineEarnings: offlineEarnings,
                    activeEarnings: validatedActiveEarnings,
                    offlineTime: {
                        seconds: offlineTimeSeconds,
                        hours: Math.floor(offlineTimeSeconds / 3600),
                        minutes: Math.floor((offlineTimeSeconds % 3600) / 60)
                    }
                },
                validation: {
                    sessionDuration: sessionDuration,
                    clicksProcessed: clickCount,
                    clickEarnings: clickEarnings,
                    clickRate: reportedClickRate,
                    maxPossibleActive: maxActiveEarnings,
                    offlineMultiplier: offlineMultiplier,
                    cheatDetection: {
                        suspiciousActivity: suspiciousActivity.carrots || suspiciousActivity.horseShoes || suspiciousActivity.goldenCarrots,
                        capped: {
                            carrots: attemptedOverEarning.carrots > 0,
                            horseShoes: attemptedOverEarning.horseShoes > 0,
                            goldenCarrots: attemptedOverEarning.goldenCarrots > 0
                        },
                        adjustedForImpossibleClickRate: reportedClickRate > maxHumanClickRate
                    }
                }
            }
        };

        // Add welcome back message for returning players with significant offline earnings
        if (isReturningPlayer && offlineTimeSeconds > 300) {
            const offlineHours = Math.floor(offlineTimeSeconds / 3600);
            const offlineMinutes = Math.floor((offlineTimeSeconds % 3600) / 60);
            
            let welcomeMessage = `Welcome back! You were away for `;
            if (offlineHours > 0) {
                welcomeMessage += `${offlineHours} hour${offlineHours > 1 ? 's' : ''}`;
                if (offlineMinutes > 0) {
                    welcomeMessage += ` and ${offlineMinutes} minute${offlineMinutes > 1 ? 's' : ''}`;
                }
            } else {
                welcomeMessage += `${offlineMinutes} minute${offlineMinutes > 1 ? 's' : ''}`;
            }
            welcomeMessage += `.`;

            response.welcomeBack = {
                message: welcomeMessage,
                offlineEarnings: offlineEarnings,
                efficiency: Math.round(offlineMultiplier * 100)
            };
        }

        res.json(response);

    } catch (error) {
        console.error('Sync session error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to sync game session'
        });
    }
});

// Check offline earnings without claiming them (protected route)
router.get('/offline-earnings', authenticateToken, async function(req, res, next) {
    try {
        // Get user data and last update time
        const userDataQuery = `
            SELECT u.Carrots, u.HorseShoes, u.G_Carrots, u.UpdatedAt,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const userDataResult = await executeQuery(userDataQuery, [req.user.id]);
        
        if (userDataResult.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User data not found'
            });
        }

        const userData = userDataResult[0];
        const lastUpdateTime = new Date(userData.UpdatedAt);
        const currentTime = new Date();
        const offlineTimeSeconds = Math.floor((currentTime - lastUpdateTime) / 1000);

        // Only calculate if offline for more than 5 minutes
        if (offlineTimeSeconds < 300) {
            return res.json({
                message: 'No offline earnings available',
                hasOfflineEarnings: false,
                offlineTime: {
                    seconds: offlineTimeSeconds,
                    minutes: Math.floor(offlineTimeSeconds / 60)
                }
            });
        }

        // Calculate offline earning rates based on upgrades
        const baseIdleRates = {
            carrots: 0.5,
            horseShoes: 0,
            goldenCarrots: 0
        };

        if (userData.Upgrade1) baseIdleRates.carrots *= 1.5;
        if (userData.Upgrade2) baseIdleRates.horseShoes = 0.05;
        if (userData.Upgrade3) baseIdleRates.goldenCarrots = 0.005;

        // Apply offline efficiency based on time away
        const offlineHours = offlineTimeSeconds / 3600;
        let efficiency = 1.0;
        
        if (offlineHours > 16) efficiency = 0.3;
        else if (offlineHours > 8) efficiency = 0.5;
        else if (offlineHours > 2) efficiency = 0.7;

        // Cap at 24 hours maximum
        const cappedOfflineTime = Math.min(offlineTimeSeconds, 24 * 3600);

        const potentialEarnings = {
            carrots: Math.floor(cappedOfflineTime * baseIdleRates.carrots * efficiency),
            horseShoes: Math.floor(cappedOfflineTime * baseIdleRates.horseShoes * efficiency),
            goldenCarrots: Math.floor(cappedOfflineTime * baseIdleRates.goldenCarrots * efficiency)
        };

        // Format time display
        const timeAway = {
            seconds: offlineTimeSeconds,
            minutes: Math.floor(offlineTimeSeconds / 60),
            hours: Math.floor(offlineTimeSeconds / 3600),
            days: Math.floor(offlineTimeSeconds / (24 * 3600))
        };

        let timeAwayText = '';
        if (timeAway.days > 0) {
            timeAwayText = `${timeAway.days} day${timeAway.days > 1 ? 's' : ''}`;
            if (timeAway.hours % 24 > 0) {
                timeAwayText += ` and ${timeAway.hours % 24} hour${timeAway.hours % 24 > 1 ? 's' : ''}`;
            }
        } else if (timeAway.hours > 0) {
            timeAwayText = `${timeAway.hours} hour${timeAway.hours > 1 ? 's' : ''}`;
            if (timeAway.minutes % 60 > 0) {
                timeAwayText += ` and ${timeAway.minutes % 60} minute${timeAway.minutes % 60 > 1 ? 's' : ''}`;
            }
        } else {
            timeAwayText = `${timeAway.minutes} minute${timeAway.minutes > 1 ? 's' : ''}`;
        }

        res.json({
            message: 'Offline earnings calculated',
            hasOfflineEarnings: true,
            offlineEarnings: potentialEarnings,
            timeAway: {
                ...timeAway,
                displayText: timeAwayText
            },
            efficiency: {
                percentage: Math.round(efficiency * 100),
                description: efficiency === 1.0 ? 'Maximum efficiency' :
                           efficiency >= 0.7 ? 'High efficiency' :
                           efficiency >= 0.5 ? 'Reduced efficiency' : 'Low efficiency'
            },
            rates: {
                perSecond: {
                    carrots: baseIdleRates.carrots * efficiency,
                    horseShoes: baseIdleRates.horseShoes * efficiency,
                    goldenCarrots: baseIdleRates.goldenCarrots * efficiency
                },
                perHour: {
                    carrots: Math.floor(3600 * baseIdleRates.carrots * efficiency),
                    horseShoes: Math.floor(3600 * baseIdleRates.horseShoes * efficiency),
                    goldenCarrots: Math.floor(3600 * baseIdleRates.goldenCarrots * efficiency)
                }
            }
        });

    } catch (error) {
        console.error('Get offline earnings error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to calculate offline earnings'
        });
    }
});

// Claim offline earnings (protected route)
router.post('/claim-offline', authenticateToken, async function(req, res, next) {
    try {
        const { watchedAd = false } = req.body;

        // Get current offline earnings calculation
        const offlineDataQuery = `
            SELECT u.Carrots, u.HorseShoes, u.G_Carrots, u.UpdatedAt,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const offlineDataResult = await executeQuery(offlineDataQuery, [req.user.id]);
        
        if (offlineDataResult.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User data not found'
            });
        }

        const userData = offlineDataResult[0];
        const lastUpdateTime = new Date(userData.UpdatedAt);
        const currentTime = new Date();
        const offlineTimeSeconds = Math.floor((currentTime - lastUpdateTime) / 1000);

        if (offlineTimeSeconds < 300) {
            return res.status(400).json({
                error: 'No offline earnings',
                message: 'Must be offline for at least 5 minutes to claim earnings'
            });
        }

        // Calculate offline earnings (same logic as preview)
        const baseIdleRates = {
            carrots: 0.5,
            horseShoes: 0,
            goldenCarrots: 0
        };

        if (userData.Upgrade1) baseIdleRates.carrots *= 1.5;
        if (userData.Upgrade2) baseIdleRates.horseShoes = 0.05;
        if (userData.Upgrade3) baseIdleRates.goldenCarrots = 0.005;

        const offlineHours = offlineTimeSeconds / 3600;
        let efficiency = 1.0;
        
        if (offlineHours > 16) efficiency = 0.3;
        else if (offlineHours > 8) efficiency = 0.5;
        else if (offlineHours > 2) efficiency = 0.7;

        const cappedOfflineTime = Math.min(offlineTimeSeconds, 24 * 3600);

        let offlineEarnings = {
            carrots: Math.floor(cappedOfflineTime * baseIdleRates.carrots * efficiency),
            horseShoes: Math.floor(cappedOfflineTime * baseIdleRates.horseShoes * efficiency),
            goldenCarrots: Math.floor(cappedOfflineTime * baseIdleRates.goldenCarrots * efficiency)
        };

        // Apply ad bonus (double earnings if watched ad)
        let adBonus = { carrots: 0, horseShoes: 0, goldenCarrots: 0 };
        if (watchedAd) {
            adBonus = {
                carrots: offlineEarnings.carrots,
                horseShoes: offlineEarnings.horseShoes,
                goldenCarrots: offlineEarnings.goldenCarrots
            };
            
            offlineEarnings = {
                carrots: offlineEarnings.carrots * 2,
                horseShoes: offlineEarnings.horseShoes * 2,
                goldenCarrots: offlineEarnings.goldenCarrots * 2
            };
        }

        // Update user currency
        const updateQuery = `
            UPDATE Users 
            SET Carrots = Carrots + ?,
                HorseShoes = HorseShoes + ?,
                G_Carrots = G_Carrots + ?,
                UpdatedAt = CURRENT_TIMESTAMP
            WHERE UserID = ?
        `;

        await executeQuery(updateQuery, [
            offlineEarnings.carrots,
            offlineEarnings.horseShoes,
            offlineEarnings.goldenCarrots,
            req.user.id
        ]);

        // Get updated totals
        const updatedQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
        const updatedResult = await executeQuery(updatedQuery, [req.user.id]);
        const updatedCurrency = updatedResult[0];

        res.json({
            message: watchedAd ? 'Offline earnings claimed with ad bonus!' : 'Offline earnings claimed!',
            claimed: {
                baseEarnings: {
                    carrots: Math.floor(offlineEarnings.carrots / (watchedAd ? 2 : 1)),
                    horseShoes: Math.floor(offlineEarnings.horseShoes / (watchedAd ? 2 : 1)),
                    goldenCarrots: Math.floor(offlineEarnings.goldenCarrots / (watchedAd ? 2 : 1))
                },
                adBonus: adBonus,
                totalClaimed: offlineEarnings
            },
            newTotals: {
                carrots: updatedCurrency.Carrots,
                horseShoes: updatedCurrency.HorseShoes,
                goldenCarrots: updatedCurrency.G_Carrots
            },
            timeOffline: {
                seconds: offlineTimeSeconds,
                hours: Math.floor(offlineTimeSeconds / 3600),
                efficiency: Math.round(efficiency * 100)
            }
        });

    } catch (error) {
        console.error('Claim offline earnings error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to claim offline earnings'
        });
    }
});

// Quick currency update for immediate actions (protected route)
router.put('/currency/quick', authenticateToken, async function(req, res, next) {
    try {
        const { action, amount = 1 } = req.body;

        // Validate action type
        const validActions = ['click', 'collect', 'spend'];
        if (!validActions.includes(action)) {
            return res.status(400).json({
                error: 'Invalid action',
                message: 'Action must be "click", "collect", or "spend"'
            });
        }

        // Validate amount
        if (!Number.isInteger(amount) || amount <= 0) {
            return res.status(400).json({
                error: 'Invalid amount',
                message: 'Amount must be a positive integer'
            });
        }

        // Anti-cheat: Prevent excessive amounts for quick actions
        const maxQuickAmounts = {
            click: 10,      // Max 10 clicks in one request
            collect: 1000,  // Max 1000 from collection
            spend: 10000    // Max 10000 spending per request
        };

        if (amount > maxQuickAmounts[action]) {
            console.warn(`🚨 EXCESSIVE QUICK ACTION - User ${req.user.id}: ${action} amount ${amount} (max: ${maxQuickAmounts[action]})`);
            return res.status(400).json({
                error: 'Amount too large',
                message: `Maximum ${action} amount is ${maxQuickAmounts[action]} per request`
            });
        }

        // Get user upgrades for multiplier calculation
        const upgradesQuery = `
            SELECT uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM User_upgrades uu
            WHERE uu.UserID = ?
        `;
        const upgradesResult = await executeQuery(upgradesQuery, [req.user.id]);
        const upgrades = upgradesResult[0] || { Upgrade1: false, Upgrade2: false, Upgrade3: false };

        let updateQuery;
        let values;

        switch (action) {
            case 'click':
                // Apply click multiplier based on upgrades
                const clickMultiplier = upgrades.Upgrade1 ? 1.5 : 1.0;
                const clickEarnings = Math.floor(amount * clickMultiplier);
                
                updateQuery = 'UPDATE Users SET Carrots = Carrots + ? WHERE UserID = ?';
                values = [clickEarnings, req.user.id];
                break;

            case 'collect':
                // For collecting idle earnings
                updateQuery = 'UPDATE Users SET Carrots = Carrots + ? WHERE UserID = ?';
                values = [amount, req.user.id];
                break;

            case 'spend':
                // For spending currency (with validation to prevent negative)
                updateQuery = 'UPDATE Users SET Carrots = GREATEST(0, Carrots - ?) WHERE UserID = ?';
                values = [amount, req.user.id];
                break;
        }

        await executeQuery(updateQuery, values);

        // Get updated currency
        const currencyQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
        const currencyResult = await executeQuery(currencyQuery, [req.user.id]);
        const newCurrency = currencyResult[0];

        res.json({
            message: `${action} processed successfully`,
            currency: {
                carrots: newCurrency.Carrots,
                horseShoes: newCurrency.HorseShoes,
                goldenCarrots: newCurrency.G_Carrots
            }
        });

    } catch (error) {
        console.error('Quick currency update error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to update currency'
        });
    }
});

// Update user currency (protected route - for admin/debug purposes)
router.put('/currency', authenticateToken, async function(req, res, next) {
    try {
        const { carrots, horseShoes, goldenCarrots, operation = 'set' } = req.body;

        // Validate operation type
        if (!['set', 'add', 'subtract'].includes(operation)) {
            return res.status(400).json({
                error: 'Invalid operation',
                message: 'Operation must be "set", "add", or "subtract"'
            });
        }

        // Validate currency values
        const currencies = { carrots, horseShoes, goldenCarrots };
        const updates = [];
        const values = [];

        for (const [currency, value] of Object.entries(currencies)) {
            if (value !== undefined) {
                if (!Number.isInteger(value) || value < 0) {
                    return res.status(400).json({
                        error: 'Invalid currency value',
                        message: `${currency} must be a non-negative integer`
                    });
                }

                const columnMap = {
                    carrots: 'Carrots',
                    horseShoes: 'HorseShoes',
                    goldenCarrots: 'G_Carrots'
                };

                const column = columnMap[currency];

                switch (operation) {
                    case 'set':
                        updates.push(`${column} = ?`);
                        values.push(value);
                        break;
                    case 'add':
                        updates.push(`${column} = ${column} + ?`);
                        values.push(value);
                        break;
                    case 'subtract':
                        updates.push(`${column} = GREATEST(0, ${column} - ?)`);
                        values.push(value);
                        break;
                }
            }
        }

        if (updates.length === 0) {
            return res.status(400).json({
                error: 'No currency updates provided',
                message: 'Please provide at least one currency value to update'
            });
        }

        // Update user currency
        values.push(req.user.id);
        const updateQuery = `UPDATE Users SET ${updates.join(', ')} WHERE UserID = ?`;
        await executeQuery(updateQuery, values);

        // Get updated currency values
        const currencyQuery = 'SELECT Carrots, HorseShoes, G_Carrots FROM Users WHERE UserID = ?';
        const currencyResult = await executeQuery(currencyQuery, [req.user.id]);
        const newCurrency = currencyResult[0];

        res.json({
            message: 'Currency updated successfully',
            currency: {
                carrots: newCurrency.Carrots,
                horseShoes: newCurrency.HorseShoes,
                goldenCarrots: newCurrency.G_Carrots
            }
        });

    } catch (error) {
        console.error('Update currency error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to update currency'
        });
    }
});

// Get upgrade costs and availability (protected route)
router.get('/upgrades/shop', authenticateToken, async function(req, res, next) {
    try {
        // Get user's current upgrades and currency
        const userDataQuery = `
            SELECT u.Carrots, u.HorseShoes, u.G_Carrots,
                   uu.Upgrade1, uu.Upgrade2, uu.Upgrade3
            FROM Users u
            LEFT JOIN User_upgrades uu ON u.UserID = uu.UserID
            WHERE u.UserID = ?
        `;
        const userDataResult = await executeQuery(userDataQuery, [req.user.id]);

        if (userDataResult.length === 0) {
            return res.status(404).json({
                error: 'User not found',
                message: 'User data not found'
            });
        }

        const userData = userDataResult[0];

        // If no upgrades record, set defaults
        if (userData.Upgrade1 === null) {
            userData.Upgrade1 = false;
            userData.Upgrade2 = false;
            userData.Upgrade3 = false;
        }

        // Define all upgrades with costs and requirements
        const allUpgrades = [
            {
                id: 1,
                name: 'Basic Upgrade',
                description: 'Increases carrot production by 50%',
                cost: { carrots: 100, horseShoes: 0, goldenCarrots: 0 },
                requires: [],
                purchased: Boolean(userData.Upgrade1)
            },
            {
                id: 2,
                name: 'Advanced Upgrade',
                description: 'Unlocks horse shoe generation',
                cost: { carrots: 500, horseShoes: 10, goldenCarrots: 0 },
                requires: [1],
                purchased: Boolean(userData.Upgrade2)
            },
            {
                id: 3,
                name: 'Premium Upgrade',
                description: 'Unlocks golden carrot rewards',
                cost: { carrots: 1000, horseShoes: 50, goldenCarrots: 5 },
                requires: [1, 2],
                purchased: Boolean(userData.Upgrade3)
            }
        ];

        // Check affordability and availability for each upgrade
        const upgradesWithStatus = allUpgrades.map(upgrade => {
            const canAfford = userData.Carrots >= upgrade.cost.carrots &&
                            userData.HorseShoes >= upgrade.cost.horseShoes &&
                            userData.G_Carrots >= upgrade.cost.goldenCarrots;

            const prerequisitesMet = upgrade.requires.every(reqId => 
                Boolean(userData[`Upgrade${reqId}`])
            );

            return {
                ...upgrade,
                available: !upgrade.purchased && prerequisitesMet,
                canAfford: canAfford && !upgrade.purchased && prerequisitesMet,
                missingPrerequisites: upgrade.requires.filter(reqId => 
                    !Boolean(userData[`Upgrade${reqId}`])
                )
            };
        });

        res.json({
            message: 'Upgrade shop retrieved successfully',
            currentCurrency: {
                carrots: userData.Carrots,
                horseShoes: userData.HorseShoes,
                goldenCarrots: userData.G_Carrots
            },
            upgrades: upgradesWithStatus
        });

    } catch (error) {
        console.error('Get upgrade shop error:', error);
        res.status(500).json({ 
            error: 'Internal server error',
            message: 'Failed to retrieve upgrade shop'
        });
    }
});

module.exports = router;
