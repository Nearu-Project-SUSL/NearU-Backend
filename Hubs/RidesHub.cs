using Microsoft.AspNetCore.SignalR;

namespace NearU_Backend_Revised.Hubs;

public class RidesHub : Hub
{
    public Task JoinRideChannel(string rideId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"ride:{rideId}");
    }

    public Task LeaveRideChannel(string rideId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride:{rideId}");
    }
}
