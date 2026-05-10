using NearU_Backend_Revised.DTOs.Rides;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Data;
using NetTopologySuite.Geometries; // Point comes from here

namespace NearU_Backend_Revised.Services
{
  public class RideService : IRideService
  {
    private readonly IRideRepository _repository;
    private readonly ApplicationDbContext _context;

    private static readonly Point UniversityLocation = new Point (80.8558, 6.3793)
    {
      SRID = 4326
      // SRID 4326 = WGS84 coordinate system (standard GPS)
    };

    private const double MaxRadiusMeters = 5000;

    public RideService(IRideRepository repository, ApplicationDbContext context)
    {
      _repository = repository;
      _context = context;
    }



    public async Task<(RideResponse? ride, string? error)> CreateRideAsync(CreateRideRequestDto dto)
    {
      //build point from dtos lat/lon values
      var pickupPoint = new Point(dto.PickupLon, dto.PickupLat){SRID = 4326};
      var dropoffPoint = new Point(dto.DropoffLon, dto.DropoffLat){SRID = 4326};

      var isWithingRadius = await _repository.IsWithinUniversityRadiusAsync (pickupPoint, UniversityLocation, MaxRadiusMeters);

      if(!isWithingRadius)
        return (null, "Pickup location is outside the 5Km university radius.");

      var ride = new RideRequest
      {
        StudentId       = dto.StudentId,
        ServiceType     = dto.ServiceType,
        Details         = dto.Details,
        PickupLocation  = pickupPoint,  // stores as PostGIS Point column
        DropoffLocation = dropoffPoint,
        Status          = "Pending"
      };

      var created = await _repository.CreateAsync(ride);
      return (MaptoResponse(created), null);
    }



    public async Task<(RideResponse? ride, string? error)> AcceptRideAsync(AcceptRideDto dto)
    {
      // Start a transaction to prevent two riders accepting the same ride (transaction = multiple db opes treated as one)
      await using var transaction = await _context.Database.BeginTransactionAsync();
      try
      {
        //row is locked while transaction is running
        var ride = await _repository.GetByIdWithLockAsync(dto.RideId);

        if(ride is null)
          return(null, "Ride is no longer available.");

          ride.RiderId   = dto.RiderId;
          ride.Status    = "Accepted";
          ride.OTP       = GenerateOtp();
          ride.UpdatedAt = DateTime.UtcNow;

          await _repository.UpdateAsync(ride);
          await transaction.CommitAsync(); //releases row lock

          return(MaptoResponse(ride), null);
      }
      catch
      {
        await transaction.RollbackAsync(); //undo everything if somthing fails
        return (null, "An error occurred while accepting the ride.");
      }
    }

    public async Task<(bool success, string? error)> VerifyOtpAsync(VerifyOtpDto dto)
    {
      var ride = await _repository.GetByIdAsync(dto.RideId);

      if(ride is null || ride.Status != "Accepted")
        return(false, "Invalid ride or ride is not in Accepted state.");

      if(ride.OTP != dto.EnteredOtp)
        return (false, "Incorrect OTP.");

      ride.Status = "Completed";
      ride.UpdatedAt = DateTime.UtcNow;

      await _repository.UpdateAsync(ride);
      return (true, null);
    }


    private static string GenerateOtp()
    {
      var bytes = new byte[4];

      //// RandomNumberGenerator uses the OS crypto API — more secure than Random
      System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

       // % 9000 gives 0-8999, + 1000 shifts to 1000-9999 = always 4 digits
      return (Math.Abs(BitConverter.ToInt32(bytes, 0)) % 9000 + 1000).ToString();
    }

    //convert riderequest model to rideresponse dto  
    private static RideResponse MaptoResponse(RideRequest ride) => new()
    {
      Id          = ride.Id,
      StudentId   = ride.StudentId,
      RiderId     = ride.RiderId,
      ServiceType = ride.ServiceType,
      Details     = ride.Details,
      Status      = ride.Status,
      OTP         = ride.OTP,
      Price       = ride.Price,
      CreatedAt   = ride.CreatedAt
    };
  }
}