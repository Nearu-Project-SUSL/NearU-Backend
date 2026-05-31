namespace NearU_Backend_Revised.Services.Interfaces;

/// <summary>
/// Manages FCM device tokens for push notification delivery.
/// Tokens are per-user-per-device and must be refreshed when Firebase rotates them.
/// </summary>
public interface IFcmTokenService
{
    /// <summary>Stores or refreshes the FCM token for a user's device.</summary>
    Task UpsertTokenAsync(string userId, string token, CancellationToken cancellationToken = default);

    /// <summary>Removes a specific token (e.g. on logout or when Firebase reports it as invalid).</summary>
    Task RemoveTokenAsync(string userId, string token, CancellationToken cancellationToken = default);

    /// <summary>Returns all stored FCM tokens for the given user IDs.</summary>
    Task<IEnumerable<string>> GetTokensForUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}
