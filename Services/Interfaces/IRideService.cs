using NearU_Backend_Revised.DTOs.Rides;

namespace NearU_Backend_Revised.Services.Interfaces
{
  public interface IRideService
  {
    Task<(RideResponse? ride, string? error)> CreateRideAsync(CreateRideRequestDto dto);
    Task<(RideResponse? ride, string? error)> AcceptRideAsync(AcceptRideDto dto);
    Task<(bool success, string? error)> VerifyOtpAsync(VerifyOtpDto dto);
}
}