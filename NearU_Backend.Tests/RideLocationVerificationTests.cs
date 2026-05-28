using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.DTOs.Ride;
using NearU_Backend_Revised.Enums;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.Services.Interfaces;
using NetTopologySuite.Geometries;
using Xunit;

namespace NearU_Backend.Tests;

public class RideLocationVerificationTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RideService _rideService;
    private readonly Mock<IRideStateMachine> _stateMachineMock = new();
    private readonly Mock<IRideNotificationService> _notificationServiceMock = new();
    private readonly Mock<ILogger<RideService>> _loggerMock = new();

    public RideLocationVerificationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        var rideSettings = Options.Create(new RideSettings
        {
            BaseFare = 50,
            RatePerKm = 20
        });

        _rideService = new RideService(
            _dbContext,
            rideSettings,
            _stateMachineMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitHeartbeatAsync_AcceptedStatus_ShouldSucceed()
    {
        // Arrange
        var riderId = "rider1";
        var rideId = "ride1";
        var studentId = "student1";
        var ride = new RideRequest
        {
            Id = rideId,
            StudentId = studentId,
            RiderId = riderId,
            Status = RideRequestStatus.Accepted,
            PickupLocation = new Point(80.7872, 6.7145) { SRID = 4326 },
            DropoffLocation = new Point(80.8000, 6.7200) { SRID = 4326 }
        };
        _dbContext.RideRequests.Add(ride);
        _dbContext.RiderStatuses.Add(new RiderStatus { RiderId = riderId, IsOnline = true });
        await _dbContext.SaveChangesAsync();

        var request = new LocationHeartbeatRequestDto
        {
            RideId = rideId,
            Latitude = 6.7150,
            Longitude = 80.7880
        };

        // Act
        var exception = await Record.ExceptionAsync(() => _rideService.SubmitHeartbeatAsync(riderId, request));

        // Assert
        Assert.Null(exception);
        var updatedRiderStatus = await _dbContext.RiderStatuses.FindAsync(riderId);
        Assert.NotNull(updatedRiderStatus.LastLocation);
        Assert.Equal(6.7150, updatedRiderStatus.LastLocation.Y);
    }

    [Fact]
    public async Task GetLiveLocationAsync_ShouldCalculateDistance()
    {
        // Arrange
        var studentId = "student1";
        var riderId = "rider1";
        var rideId = "ride1";
        var ride = new RideRequest
        {
            Id = rideId,
            StudentId = studentId,
            RiderId = riderId,
            Status = RideRequestStatus.Accepted,
            PickupLocation = new Point(80.7872, 6.7145) { SRID = 4326 },
            DropoffLocation = new Point(80.8000, 6.7200) { SRID = 4326 }
        };
        _dbContext.RideRequests.Add(ride);
        _dbContext.RiderStatuses.Add(new RiderStatus 
        { 
            RiderId = riderId, 
            LastLocation = new Point(80.7880, 6.7150) { SRID = 4326 },
            LastSeen = DateTime.UtcNow 
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _rideService.GetLiveLocationAsync(studentId, rideId);

        // Assert
        Assert.NotNull(result.DistanceToPickupKm);
        Assert.True(result.DistanceToPickupKm > 0);
    }
}
