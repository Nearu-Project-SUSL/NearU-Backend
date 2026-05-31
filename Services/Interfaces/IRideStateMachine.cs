using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.Services.Interfaces;

public interface IRideStateMachine
{
    bool CanTransition(RideRequestStatus from, RideRequestStatus to);
    void EnsureTransition(RideRequestStatus from, RideRequestStatus to);
}
