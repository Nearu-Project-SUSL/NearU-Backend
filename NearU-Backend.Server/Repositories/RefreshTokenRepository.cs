using Microsoft.EntityFrameworkCore;
using NearU_Backend.Server.Data;
using NearU_Backend.Server.Models;
using NearU_Backend.Server.Repositories.Interfaces;

namespace NearU_Backend.Server.Repositories;

/// <summary>
/// Repository implementation for RefreshToken operations.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NearUDbContext _context;

    public RefreshTokenRepository(NearUDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<IEnumerable<RefreshToken>> GetAllTokensByUserIdAsync(int userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var token = await _context.RefreshTokens.FindAsync(id);
        if (token == null)
            return false;

        _context.RefreshTokens.Remove(token);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason)
    {
        var refreshToken = await GetByTokenAsync(token);
        if (refreshToken == null || !refreshToken.IsActive)
            return false;

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = reason;

        await UpdateAsync(refreshToken);
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(int userId, string ipAddress, string reason)
    {
        var activeTokens = await GetActiveTokensByUserIdAsync(userId);
        
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();

        return expiredTokens.Count;
    }
}
