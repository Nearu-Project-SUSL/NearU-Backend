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
    private readonly GeometryFactory _geometryFactory;

    public RideService(
        ApplicationDbContext dbContext,
        IOptions<RideSettings> rideSettings,
        IRideStateMachine stateMachine,
        IRideNotificationService rideNotificationService)
    {
        _dbContext = dbContext;
        _rideSettings = rideSettings.Value;
        _stateMachine = stateMachine;
        _rideNotificationService = rideNotificationService;
        _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<RideSummaryDto> CreateRequestAsync(string studentId, CreateRideRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!request.ConfirmEstimate)
        {
            throw new InvalidOperationException("Binding estimate must be confirmed before submitting a request.");
        }

        var pickup = CreatePoint(request.PickupLongitude, request.PickupLatitude);
        var dropoff = CreatePoint(request.DropoffLongitude, request.DropoffLatitude);

        var pickupInside = await IsWithinFacultyRadiusAsync(pickup, cancellationToken);
        var dropoffInside = await IsWithinFacultyRadiusAsync(dropoff, cancellationToken);
        if (!pickupInside || !dropoffInside)
        {
            throw new InvalidOperationException("Pickup and drop-off points must be within the 5 km operational boundary.");
        }

        var distanceKm = CalculateDistanceKm(
            request.PickupLatitude,
            request.PickupLongitude,
            request.DropoffLatitude,
            request.DropoffLongitude);

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

        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);
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
        if (ride.Status != RideRequestStatus.InProgress && ride.Status != RideRequestStatus.Arrived)
        {
            throw new InvalidOperationException("OTP can only be verified for rides that are in progress.");
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

        _stateMachine.EnsureTransition(ride.Status, RideRequestStatus.Completed);
        ride.Status = RideRequestStatus.Completed;
        ride.CompletedAt = DateTime.UtcNow;
        ride.UpdatedAt = DateTime.UtcNow;

        if (!await _dbContext.RideHistories.AnyAsync(h => h.RideId == ride.Id, cancellationToken))
        {
            _dbContext.RideHistories.Add(new RideHistory
            {
                RideId = ride.Id,
                StudentId = ride.StudentId,
                RiderId = ride.RiderId,
                ServiceType = ride.ServiceType,
                FinalFare = ride.EstimatedFare,
                CalculatedDistance = ride.CalculatedDistance,
                CreatedAt = ride.CreatedAt,
                CompletedAt = ride.CompletedAt ?? DateTime.UtcNow
            });
        }

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

    public async Task SubmitHeartbeatAsync(string riderId, LocationHeartbeatRequestDto request, CancellationToken cancellationToken = default)
    {
        var ride = await GetRideOwnedByRiderAsync(riderId, request.RideId, cancellationToken);
        if (ride.Status != RideRequestStatus.Arrived && ride.Status != RideRequestStatus.InProgress)
        {
            throw new InvalidOperationException("Heartbeats are only accepted for arrived/in-progress rides.");
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
        await _rideNotificationService.NotifyStateChangeAsync(ride, cancellationToken);
        await _rideNotificationService.BroadcastLocationAsync(ride.Id, point.Y, point.X, cancellationToken);
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

        return new RideLocationResponseDto
        {
            RideId = ride.Id,
            Latitude = riderStatus.LastLocation.Y,
            Longitude = riderStatus.LastLocation.X,
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
    public Task<FareEstimateResponseDto> EstimateFareAsync(
        double pickupLat, double pickupLng,
        double dropoffLat, double dropoffLng,
        CancellationToken cancellationToken = default)
    {
        var distanceKm = CalculateDistanceKm(pickupLat, pickupLng, dropoffLat, dropoffLng);
        var estimatedFare = _rideSettings.BaseFare + ((decimal)distanceKm * _rideSettings.RatePerKm);

        return Task.FromResult(new FareEstimateResponseDto
        {
            DistanceKm = decimal.Round((decimal)distanceKm, 3, MidpointRounding.AwayFromZero),
            EstimatedFare = decimal.Round(estimatedFare, 2, MidpointRounding.AwayFromZero),
            BaseFare = _rideSettings.BaseFare,
            RatePerKm = _rideSettings.RatePerKm
        });
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

    private async Task<bool> IsWithinFacultyRadiusAsync(Point point, CancellationToken cancellationToken)
    {
        await using var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ST_DWithin(
                ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography,
                ST_SetSRID(ST_MakePoint(@centerLng, @centerLat), 4326)::geography,
                @radiusMeters
            );
            """;

        AddParameter(command, "lng", point.X);
        AddParameter(command, "lat", point.Y);
        AddParameter(command, "centerLng", RideConstants.FacultyCentroidLng);
        AddParameter(command, "centerLat", RideConstants.FacultyCentroidLat);
        AddParameter(command, "radiusMeters", RideConstants.AllowedRadiusMeters);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool flag && flag;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

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
