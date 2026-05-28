using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Constants;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.DTOs.Ride;
using NearU_Backend_Revised.Enums;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services;

public class RideService : IRideService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RideSettings _rideSettings;
    private readonly IRideStateMachine _stateMachine;
    private readonly IRideNotificationService _rideNotificationService;
    private readonly IOsrmService _osrm;
    private readonly GeometryFactory _geometryFactory;
    private readonly ILogger<RideService> _logger;

    public RideService(
        ApplicationDbContext dbContext,
        IOptions<RideSettings> rideSettings,
        IRideStateMachine stateMachine,
        IRideNotificationService rideNotificationService,
        IOsrmService osrm,
        ILogger<RideService> logger)
    {
        _dbContext = dbContext;
        _rideSettings = rideSettings.Value;
        _stateMachine = stateMachine;
        _rideNotificationService = rideNotificationService;
        _osrm = osrm;
        _logger = logger;
        _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    /// <summary>
    /// Returns the active (non-terminal) ride for the given user — works for both students and riders.
    /// Returns null if no active ride exists. Used by mobile clients on app relaunch to rehydrate state.
    /// </summary>
    public async Task<RideSummaryDto?> GetActiveRideAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Terminal statuses — anything else is "active"
        var terminalStatuses = new[]
        {
            RideRequestStatus.Completed,
            RideRequestStatus.Cancelled,
            RideRequestStatus.Interrupted,
            RideRequestStatus.Expired,
            RideRequestStatus.OTPLocked
        };

        var ride = await _dbContext.RideRequests
            .Where(r =>
                (r.StudentId == userId || r.RiderId == userId) &&
                !terminalStatuses.Contains(r.Status))
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return ride is null ? null : MapSummary(ride);
    }

    public async Task<RideSummaryDto> CreateRequestAsync(string studentId, CreateRideRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!request.ConfirmEstimate)
        {
            throw new InvalidOperationException("Binding estimate must be confirmed before submitting a request.");
        }

        var pickup = CreatePoint(request.PickupLongitude, request.PickupLatitude);
        var dropoff = CreatePoint(request.DropoffLongitude, request.DropoffLatitude);

        var pickupInside  = IsWithinFacultyRadius(pickup);
        var dropoffInside = IsWithinFacultyRadius(dropoff);
        if (!pickupInside || !dropoffInside)
        {
            throw new InvalidOperationException("Pickup and drop-off points must be within the 5 km operational boundary.");
        }

        // Use OSRM for true road-network distance (falls back to Haversine on failure)
        var distanceKm = await _osrm.GetRoadDistanceKmAsync(
            request.PickupLatitude,
            request.PickupLongitude,
            request.DropoffLatitude,
            request.DropoffLongitude,
            cancellationToken);

        var estimatedFare = _rideSettings.BaseFare + ((decimal)distanceKm * _rideSettings.RatePerKm);
        var now = DateTime.UtcNow;

        var ride = new RideRequest
        {
            Id = Guid.NewGuid().ToString(),
            StudentId = studentId,
            ServiceType = request.ServiceType,
            Details = request.Details.GetRawText(),
            Status = RideRequestStatus.Pending,
            PickupLocation = pickup,
            DropoffLocation = dropoff,
            EstimatedFare = decimal.Round(estimatedFare, 2, MidpointRounding.AwayFromZero),
            CalculatedDistance = decimal.Round((decimal)distanceKm, 3, MidpointRounding.AwayFromZero),
            PriceRateSnapshot = JsonSerializer.Serialize(new
            {
                _rideSettings.BaseFare,
                _rideSettings.RatePerKm,
                CapturedAtUtc = now
            }),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.RideRequests.Add(ride);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Notify online riders via SignalR (app is open)
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);

        // Push to riders whose app is backgrounded/closed — fire-and-forget so it never delays the response
        _ = Task.Run(async () =>
        {
            try
            {
                var nearbyRiderIds = await _dbContext.RiderStatuses
                    .Where(rs =>
                        rs.IsOnline &&
                        rs.ApprovalStatus == RiderApprovalStatus.Approved &&
                        rs.RiderId != studentId)
                    .Select(rs => rs.RiderId)
                    .ToListAsync(CancellationToken.None);

                if (nearbyRiderIds.Any())
                    await _rideNotificationService.SendNewRideRequestPushAsync(ride, nearbyRiderIds, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background FCM push failed for ride {RideId}", ride.Id);
            }
        }, CancellationToken.None);

        return MapSummary(ride);
    }

    public async Task<RideSummaryDto> AcceptAsync(string riderId, string rideId, CancellationToken cancellationToken = default)
    {
        var riderStatus = await _dbContext.RiderStatuses.FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);
        if (riderStatus == null || !riderStatus.IsOnline || riderStatus.ApprovalStatus != RiderApprovalStatus.Approved)
        {
            throw new InvalidOperationException("Only approved online riders can accept requests.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var lockedRide = await _dbContext.RideRequests
            .FromSqlInterpolated($@"SELECT * FROM ""RideRequests"" WHERE ""Id"" = {rideId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (lockedRide == null)
        {
            throw new InvalidOperationException("Ride request not found.");
        }

        _stateMachine.EnsureTransition(lockedRide.Status, RideRequestStatus.Accepted);

        lockedRide.RiderId = riderId;
        lockedRide.Status = RideRequestStatus.Accepted;
        lockedRide.OTP = GenerateOtp();
        lockedRide.OtpExpiresAt = DateTime.UtcNow.AddMinutes(RideConstants.OtpExpiryMinutes);
        lockedRide.OTPAttempts = 0;
        lockedRide.AcceptedAt = DateTime.UtcNow;
        lockedRide.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _rideNotificationService.NotifyStateChangeAsync(lockedRide, cancellationToken);

        return MapSummary(lockedRide);
    }

    public async Task<RideSummaryDto> ArriveAsync(string riderId, string rideId, CancellationToken cancellationToken = default)
    {
        var ride = await GetRideOwnedByRiderAsync(riderId, rideId, cancellationToken);
        _stateMachine.EnsureTransition(ride.Status, RideRequestStatus.Arrived);

        ride.Status = RideRequestStatus.Arrived;
        ride.ArrivedAt = DateTime.UtcNow;
        ride.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);

        return MapSummary(ride);
    }

    public async Task<RideSummaryDto> VerifyAsync(string riderId, string rideId, string otp, CancellationToken cancellationToken = default)
    {
        var ride = await GetRideOwnedByRiderAsync(riderId, rideId, cancellationToken);
        if (ride.Status != RideRequestStatus.Arrived && ride.Status != RideRequestStatus.Accepted)
        {
            throw new InvalidOperationException("OTP can only be verified before the ride starts.");
        }

        if (ride.OtpExpiresAt is null || ride.OtpExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("OTP has expired.");
        }

        if (!string.Equals(ride.OTP, otp, StringComparison.Ordinal))
        {
            ride.OTPAttempts += 1;
            ride.UpdatedAt = DateTime.UtcNow;

            if (ride.OTPAttempts >= RideConstants.MaxOtpAttempts)
            {
                _stateMachine.EnsureTransition(ride.Status, RideRequestStatus.OTPLocked);
                ride.Status = RideRequestStatus.OTPLocked;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);
            throw new InvalidOperationException("Invalid OTP.");
        }

        ride.Status = RideRequestStatus.InProgress;
        ride.InProgressAt = DateTime.UtcNow;
        ride.OTPAttempts = 0;
        ride.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);

        return MapSummary(ride);
    }

    public async Task<RideSummaryDto> RefreshOtpAsync(string studentId, string rideId, CancellationToken cancellationToken = default)
    {
        var ride = await _dbContext.RideRequests.FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken)
            ?? throw new InvalidOperationException("Ride request not found.");
        if (ride.StudentId != studentId)
        {
            throw new UnauthorizedAccessException("Only the matched student can refresh OTP.");
        }

        if (ride.Status != RideRequestStatus.Accepted && ride.Status != RideRequestStatus.Arrived && ride.Status != RideRequestStatus.InProgress)
        {
            throw new InvalidOperationException("OTP refresh is only available for active rides.");
        }

        ride.OTP = GenerateOtp();
        ride.OtpExpiresAt = DateTime.UtcNow.AddMinutes(RideConstants.OtpExpiryMinutes);
        ride.OTPAttempts = 0;
        ride.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapSummary(ride);
    }

    public async Task<RideSummaryDto> CancelAsync(string studentId, string rideId, CancellationToken cancellationToken = default)
    {
        var ride = await _dbContext.RideRequests.FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken)
            ?? throw new InvalidOperationException("Ride request not found.");
        if (ride.StudentId != studentId)
        {
            throw new UnauthorizedAccessException("Only the request owner can cancel the ride.");
        }

        if (ride.Status != RideRequestStatus.Pending && ride.Status != RideRequestStatus.Accepted && ride.Status != RideRequestStatus.Arrived)
        {
            throw new InvalidOperationException("Only pending/accepted/arrived rides can be cancelled.");
        }

        _stateMachine.EnsureTransition(ride.Status, RideRequestStatus.Cancelled);
        var now = DateTime.UtcNow;
        var shouldApplyPenalty = ride.AcceptedAt.HasValue &&
                                 (now - ride.AcceptedAt.Value).TotalSeconds > RideConstants.CancellationWindowSeconds;

        ride.Status = RideRequestStatus.Cancelled;
        ride.CancelledAt = now;
        ride.PenaltyApplied = shouldApplyPenalty;
        ride.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);
        return MapSummary(ride);
    }

    public async Task SetRiderStatusAsync(string riderId, bool isOnline, CancellationToken cancellationToken = default)
    {
        var riderStatus = await _dbContext.RiderStatuses.FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);
        if (riderStatus == null)
        {
            riderStatus = new RiderStatus
            {
                RiderId = riderId,
                IsOnline = isOnline,
                LastSeen = DateTime.UtcNow
            };
            _dbContext.RiderStatuses.Add(riderStatus);
        }
        else
        {
            riderStatus.IsOnline = isOnline;
            riderStatus.LastSeen = DateTime.UtcNow;
        }

        if (riderStatus.ApprovalStatus != RiderApprovalStatus.Approved && isOnline)
        {
            throw new InvalidOperationException("Rider mode is restricted to approved riders.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RiderStatus?> GetRiderStatusAsync(string riderId, CancellationToken cancellationToken = default)
    {
        var riderStatus = await _dbContext.RiderStatuses.FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);
        if (riderStatus == null)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == riderId && u.Role == "Rider", cancellationToken);
            if (user != null)
            {
                riderStatus = new RiderStatus
                {
                    RiderId = riderId,
                    IsOnline = false,
                    ApprovalStatus = RiderApprovalStatus.Pending,
                    LastSeen = DateTime.UtcNow
                };
                _dbContext.RiderStatuses.Add(riderStatus);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        return riderStatus;
    }

    public async Task SubmitHeartbeatAsync(string riderId, LocationHeartbeatRequestDto request, CancellationToken cancellationToken = default)
    {
        var ride = await GetRideOwnedByRiderAsync(riderId, request.RideId, cancellationToken);
        if (ride.Status != RideRequestStatus.Accepted && 
            ride.Status != RideRequestStatus.Arrived && 
            ride.Status != RideRequestStatus.InProgress)
        {
            throw new InvalidOperationException("Heartbeats are only accepted for accepted/arrived/in-progress rides.");
        }

        if (ride.Status == RideRequestStatus.Arrived)
        {
            _stateMachine.EnsureTransition(ride.Status, RideRequestStatus.InProgress);
            ride.Status = RideRequestStatus.InProgress;
            ride.InProgressAt = DateTime.UtcNow;
        }

        var point = CreatePoint(request.Longitude, request.Latitude);
        var now = DateTime.UtcNow;
        ride.LastHeartbeatAt = now;
        ride.UpdatedAt = now;

        var riderStatus = await _dbContext.RiderStatuses.FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);
        if (riderStatus != null)
        {
            riderStatus.LastLocation = point;
            riderStatus.LastSeen = now;
        }

        _dbContext.TrackingLogs.Add(new TrackingLog
        {
            RideId = ride.Id,
            Coordinates = point,
            Timestamp = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Road distance from the rider's current position to the pickup point
        var distanceToPickup = await _osrm.GetRoadDistanceKmAsync(
            point.Y, point.X,
            ride.PickupLocation.Y, ride.PickupLocation.X,
            cancellationToken);

        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);
        await _rideNotificationService.BroadcastLocationAsync(ride.Id, point.Y, point.X, (decimal)distanceToPickup, cancellationToken);
    }

    public async Task<RideLocationResponseDto> GetLiveLocationAsync(string studentId, string rideId, CancellationToken cancellationToken = default)
    {
        var ride = await _dbContext.RideRequests.FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken)
            ?? throw new InvalidOperationException("Ride request not found.");
        if (ride.StudentId != studentId)
            throw new UnauthorizedAccessException("Live location is restricted to the matched student.");

        if (string.IsNullOrWhiteSpace(ride.RiderId))
            throw new InvalidOperationException("Ride has no assigned rider yet.");

        var riderStatus = await _dbContext.RiderStatuses.FirstOrDefaultAsync(rs => rs.RiderId == ride.RiderId, cancellationToken)
            ?? throw new InvalidOperationException("Rider status not found.");
        if (riderStatus.LastLocation == null)
            throw new InvalidOperationException("No live coordinates available yet.");

        var distanceKm = await _osrm.GetRoadDistanceKmAsync(
            riderStatus.LastLocation.Y,
            riderStatus.LastLocation.X,
            ride.PickupLocation.Y,
            ride.PickupLocation.X,
            cancellationToken);

        return new RideLocationResponseDto
        {
            RideId = ride.Id,
            Latitude = riderStatus.LastLocation.Y,
            Longitude = riderStatus.LastLocation.X,
            DistanceToPickupKm = decimal.Round((decimal)distanceKm, 3, MidpointRounding.AwayFromZero),
            UpdatedAtUtc = riderStatus.LastSeen
        };
    }

    public async Task<IEnumerable<RideSummaryDto>> GetNearbyRequestsAsync(string riderId, double latitude, double longitude, double radiusMeters = 5000, CancellationToken cancellationToken = default)
    {
        var riderStatus = await _dbContext.RiderStatuses.FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);
        if (riderStatus == null || !riderStatus.IsOnline || riderStatus.ApprovalStatus != RiderApprovalStatus.Approved)
        {
            throw new InvalidOperationException("Only approved online riders can search for requests.");
        }

        var point = CreatePoint(longitude, latitude);

        var pendingRides = await _dbContext.RideRequests
            .Where(r => r.Status == RideRequestStatus.Pending)
            .Where(r => r.PickupLocation.Distance(point) <= radiusMeters)
            .OrderBy(r => r.PickupLocation.Distance(point))
            .Take(20)
            .ToListAsync(cancellationToken);

        return pendingRides.Select(MapSummary);
    }

    /// <summary>
    /// Pure fare calculation — does not create any database record.
    /// Used by the client to show an estimate before the student confirms.
    /// </summary>
    public async Task<FareEstimateResponseDto> EstimateFareAsync(
        double pickupLat, double pickupLng,
        double dropoffLat, double dropoffLng,
        CancellationToken cancellationToken = default)
    {
        var pickup = CreatePoint(pickupLng, pickupLat);
        var dropoff = CreatePoint(dropoffLng, dropoffLat);

        if (!IsWithinFacultyRadius(pickup) || !IsWithinFacultyRadius(dropoff))
        {
            throw new InvalidOperationException("Pickup and drop-off points must be within the 5 km operational boundary.");
        }

        // Use OSRM for true road-network distance (falls back to Haversine on failure)
        var (distanceKm, durationSecs) = await _osrm.GetRouteAsync(
            pickupLat, pickupLng, dropoffLat, dropoffLng, cancellationToken);

        var estimatedFare = _rideSettings.BaseFare + ((decimal)distanceKm * _rideSettings.RatePerKm);

        return new FareEstimateResponseDto
        {
            DistanceKm = decimal.Round((decimal)distanceKm, 3, MidpointRounding.AwayFromZero),
            EstimatedFare = decimal.Round(estimatedFare, 2, MidpointRounding.AwayFromZero),
            BaseFare = _rideSettings.BaseFare,
            RatePerKm = _rideSettings.RatePerKm,
            EstimatedDurationSeconds = (int)Math.Round(durationSecs)
        };
    }

    /// <summary>
    /// Returns paginated ride history for the authenticated user (works for both students and riders).
    /// </summary>
    public async Task<IEnumerable<RideHistoryDto>> GetHistoryAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        var history = await _dbContext.RideHistories
            .Where(h => h.StudentId == userId || h.RiderId == userId)
            .OrderByDescending(h => h.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return history.Select(h => new RideHistoryDto
        {
            HistoryId = h.Id,
            RideId = h.RideId,
            StudentId = h.StudentId,
            RiderId = h.RiderId,
            ServiceType = h.ServiceType,
            FinalFare = h.FinalFare,
            DistanceKm = h.CalculatedDistance,
            CompletedAt = h.CompletedAt,
            RiderRating = h.RiderRating,
            StudentRating = h.StudentRating
        });
    }

    /// <summary>
    /// Allows either party to submit a rating (1–5) for a completed ride.
    /// A student rates the rider; a rider rates the student.
    /// </summary>
    public async Task RateRideAsync(string userId, string rideId, int rating, CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        var history = await _dbContext.RideHistories.FirstOrDefaultAsync(h => h.RideId == rideId, cancellationToken)
            ?? throw new InvalidOperationException("Ride history record not found. Only completed rides can be rated.");

        if (history.StudentId == userId)
        {
            if (history.RiderRating.HasValue)
                throw new InvalidOperationException("You have already rated this ride.");
            history.RiderRating = rating;
        }
        else if (history.RiderId == userId)
        {
            if (history.StudentRating.HasValue)
                throw new InvalidOperationException("You have already rated this ride.");
            history.StudentRating = rating;
        }
        else
        {
            throw new UnauthorizedAccessException("You were not a participant in this ride.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RideSummaryDto> RiderCompleteAsync(string riderId, string rideId, CancellationToken cancellationToken = default)
    {
        var ride = await GetRideOwnedByRiderAsync(riderId, rideId, cancellationToken);

        if(ride.Status != RideRequestStatus.InProgress)
            throw new InvalidOperationException("Ride must be in progress to mark as complete.");

        ride.Status = RideRequestStatus.CompletedByRider;
        ride.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Notify about the state change to student (ask if the ride is complete)
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);

        return MapSummary(ride);
    }

    public async Task<(bool success, string? error)> StudentConfirmCompleteAsync(string studentId, string rideId, CancellationToken cancellationToken = default)
    {
        var ride = await _dbContext.RideRequests
            .FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken);

        if (ride is null || ride.StudentId != studentId)
            return (false, "Ride not found or you are not the student for this ride.");
        if (ride.Status != RideRequestStatus.CompletedByRider)
            return (false, "Ride has not been marked complete by the rider yet.");

        ride.Status = RideRequestStatus.Completed;
        ride.CompletedAt = DateTime.UtcNow;
        ride.UpdatedAt = DateTime.UtcNow;

        // Create history record if it doesn't exist yet (idempotent)
        if (!await _dbContext.RideHistories.AnyAsync(h => h.RideId == rideId, cancellationToken))
        {
            _dbContext.RideHistories.Add(new RideHistory
            {
                RideId             = ride.Id,
                StudentId          = ride.StudentId,
                RiderId            = ride.RiderId,
                ServiceType        = ride.ServiceType,
                FinalFare          = ride.EstimatedFare,
                CalculatedDistance = ride.CalculatedDistance,
                CreatedAt          = ride.CreatedAt,
                CompletedAt        = ride.CompletedAt ?? DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);

        return (true, null);
    }

    public async Task<object> GetRiderStatsAsync(string riderId, CancellationToken cancellationToken = default)
    {
        var totalRides = await _dbContext.RideHistories
            .CountAsync(h => h.RiderId == riderId, cancellationToken);

        var todayStart = DateTime.UtcNow.Date;
        var todayEarnings = await _dbContext.RideHistories
            .Where(h => h.RiderId == riderId && h.CompletedAt >= todayStart)
            .SumAsync(h => h.FinalFare, cancellationToken);

        var ratings = await _dbContext.RideHistories
            .Where(h => h.RiderId == riderId && h.RiderRating.HasValue)
            .Select(h => h.RiderRating!.Value)
            .ToListAsync(cancellationToken);

        var averageRating = ratings.Any() ? ratings.Average() : 5.0;

        return new
        {
            totalRides = totalRides,
            todayEarnings = todayEarnings,
            rating = averageRating
        };
    }

    private async Task<RideRequest> GetRideOwnedByRiderAsync(string riderId, string rideId, CancellationToken cancellationToken)
    {
        var ride = await _dbContext.RideRequests.FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken)
            ?? throw new InvalidOperationException("Ride request not found.");
        if (ride.RiderId != riderId)
        {
            throw new UnauthorizedAccessException("Only the matched rider can perform this action.");
        }

        return ride;
    }

    private static RideSummaryDto MapSummary(RideRequest ride)
    {
        return new RideSummaryDto
        {
            RideId = ride.Id,
            Status = ride.Status,
            ServiceType = ride.ServiceType,
            StudentId = ride.StudentId,
            RiderId = ride.RiderId,
            EstimatedFare = ride.EstimatedFare,
            DistanceKm = ride.CalculatedDistance,
            // PostGIS uses (X=Longitude, Y=Latitude) convention
            PickupLatitude = ride.PickupLocation?.Y ?? 0,
            PickupLongitude = ride.PickupLocation?.X ?? 0,
            DropoffLatitude = ride.DropoffLocation?.Y ?? 0,
            DropoffLongitude = ride.DropoffLocation?.X ?? 0,
            CreatedAt = ride.CreatedAt,
            OtpExpiresAt = ride.OtpExpiresAt
        };
    }

    private static string GenerateOtp()
    {
        return Random.Shared.Next(1000, 10000).ToString(CultureInfo.InvariantCulture);
    }

    private Point CreatePoint(double longitude, double latitude)
    {
        return _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
    }

    private static bool IsWithinFacultyRadius(Point point)
    {
        const double R = 6371000;
        var dLat = ToRadians(RideConstants.FacultyCentroidLat - point.Y);
        var dLon = ToRadians(RideConstants.FacultyCentroidLng - point.X);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRadians(point.Y)) * Math.Cos(ToRadians(RideConstants.FacultyCentroidLat))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c <= RideConstants.AllowedRadiusMeters;
    }
    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    // NOTE: CalculateDistanceKm (Haversine) is kept for use in IsWithinFacultyRadius
    // and as the OSRM fallback — see OsrmService.HaversineKm for the live fallback.
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180);
}
