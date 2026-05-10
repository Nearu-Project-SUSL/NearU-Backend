using NearU_Backend_Revised.DTOs.Rides;

namespace NearU_Backend_Revised.Services.Interfaces
{
  public interface IRideService
  {
    Task<(RideResponse? ride, string? error)> CreateRideAsync(CreateRideRequest dto);
    Task<(RideResponse? ride, string? error)> AcceptRideAsync(AcceptRide dto);
    Task<(bool success, string? error)> VerifyOtpAsync(VerifyOtp dto);
}
}