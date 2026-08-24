using NearU_Backend_Revised.Enums;
using NearU_Backend_Revised.Services;
using Xunit;

namespace NearU_Backend.Tests;

public class RideStateMachineTests
{
    private readonly RideStateMachine _stateMachine = new();

    [Fact]
    public void CanTransition_PendingToAccepted_ReturnsTrue()
    {
        Assert.True(_stateMachine.CanTransition(RideRequestStatus.Pending, RideRequestStatus.Accepted));
    }

    [Fact]
    public void EnsureTransition_CompletedToPending_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => _stateMachine.EnsureTransition(RideRequestStatus.Completed, RideRequestStatus.Pending));
    }

    [Fact]
    public void CanTransition_InProgressToInterrupted_ReturnsTrue()
    {
        Assert.True(_stateMachine.CanTransition(RideRequestStatus.InProgress, RideRequestStatus.Interrupted));
    }
}
