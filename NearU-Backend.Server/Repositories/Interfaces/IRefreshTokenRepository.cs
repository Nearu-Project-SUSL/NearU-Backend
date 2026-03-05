using NearU_Backend.Server.Models;

namespace NearU_Backend.Server.Repositories.Interfaces;

/// <summary>
/// Repository interface for RefreshToken operations.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId);
    Task<IEnumerable<RefreshToken>> GetAllTokensByUserIdAsync(int userId);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);
    Task<bool> DeleteAsync(int id);
    Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason);
    Task<bool> RevokeAllUserTokensAsync(int userId, string ipAddress, string reason);
    Task<int> DeleteExpiredTokensAsync();
}
