using NearU_Backend_Revised.Enums;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services;

public class RideStateMachine : IRideStateMachine
{
    private static readonly Dictionary<RideRequestStatus, HashSet<RideRequestStatus>> Transitions = new()
    {
        [RideRequestStatus.Pending] = new HashSet<RideRequestStatus>
        {
            RideRequestStatus.Accepted,
            RideRequestStatus.Cancelled,
            RideRequestStatus.Expired
        },
        [RideRequestStatus.Accepted] = new HashSet<RideRequestStatus>
        {
            RideRequestStatus.Arrived,
            RideRequestStatus.Cancelled,
            RideRequestStatus.OTPLocked
        },
        [RideRequestStatus.Arrived] = new HashSet<RideRequestStatus>
        {
            RideRequestStatus.InProgress,
            RideRequestStatus.Cancelled,
            RideRequestStatus.OTPLocked
        },
        [RideRequestStatus.InProgress] = new HashSet<RideRequestStatus>
        {
            RideRequestStatus.Completed,
            RideRequestStatus.Interrupted,
            RideRequestStatus.OTPLocked
        },
        [RideRequestStatus.Completed] = new HashSet<RideRequestStatus>(),
        [RideRequestStatus.Cancelled] = new HashSet<RideRequestStatus>(),
        [RideRequestStatus.Interrupted] = new HashSet<RideRequestStatus>(),
        [RideRequestStatus.OTPLocked] = new HashSet<RideRequestStatus>(),
        [RideRequestStatus.Expired] = new HashSet<RideRequestStatus>()
    };

    public bool CanTransition(RideRequestStatus from, RideRequestStatus to)
    {
        return Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public void EnsureTransition(RideRequestStatus from, RideRequestStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException($"Invalid state transition from '{from}' to '{to}'.");
        }
    }
}
