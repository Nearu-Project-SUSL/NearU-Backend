using NearU_Backend_Revised.DTOs.Ride;
using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Services.Interfaces;

public interface IRideService
{
    Task<FareEstimateResponseDto> EstimateFareAsync(double pickupLat, double pickupLng, double dropoffLat, double dropoffLng, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> CreateRequestAsync(string studentId, CreateRideRequestDto request, CancellationToken cancellationToken = default);
    Task<RideSummaryDto?> GetActiveRideAsync(string userId, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> AcceptAsync(string riderId, string rideId, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> ArriveAsync(string riderId, string rideId, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> VerifyAsync(string riderId, string rideId, string otp, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> RefreshOtpAsync(string studentId, string rideId, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> CancelAsync(string studentId, string rideId, CancellationToken cancellationToken = default);
    Task SetRiderStatusAsync(string riderId, bool isOnline, CancellationToken cancellationToken = default);
    Task<RiderStatus?> GetRiderStatusAsync(string riderId, CancellationToken cancellationToken = default);
    Task SubmitHeartbeatAsync(string riderId, LocationHeartbeatRequestDto request, CancellationToken cancellationToken = default);
    Task<RideLocationResponseDto> GetLiveLocationAsync(string studentId, string rideId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RideSummaryDto>> GetNearbyRequestsAsync(string riderId, double latitude, double longitude, double radiusMeters = 5000, CancellationToken cancellationToken = default);
    Task<IEnumerable<RideHistoryDto>> GetHistoryAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task RateRideAsync(string userId, string rideId, int rating, CancellationToken cancellationToken = default);
    Task<RideSummaryDto> RiderCompleteAsync(string riderId, string rideId, CancellationToken cancellationToken = default);
    Task<(bool success, string? error)> StudentConfirmCompleteAsync(string studentId, string rideId, CancellationToken cancellationToken = default);
    Task<object> GetRiderStatsAsync(string riderId, CancellationToken cancellationToken = default);
}
