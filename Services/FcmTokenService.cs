using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services;

public class FcmTokenService : IFcmTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<FcmTokenService> _logger;

    public FcmTokenService(ApplicationDbContext dbContext, ILogger<FcmTokenService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task UpsertTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserFcmTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (existing is not null)
        {
            // Token already stored — just bump the LastSeenAt timestamp
            existing.LastSeenAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.UserFcmTokens.Add(new UserFcmToken
            {
                UserId     = userId,
                Token      = token,
                LastSeenAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("FCM token upserted for user {UserId}", userId);
    }

    public async Task RemoveTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserFcmTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token, cancellationToken);

        if (existing is not null)
        {
            _dbContext.UserFcmTokens.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("FCM token removed for user {UserId}", userId);
        }
    }

    public async Task<IEnumerable<string>> GetTokensForUsersAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        if (!ids.Any()) return Enumerable.Empty<string>();

        return await _dbContext.UserFcmTokens
            .Where(t => ids.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);
    }
}
