namespace NearU_Backend_Revised.Services.Interfaces;

/// <summary>
/// Calculates road-network distances and durations using the OSRM routing engine.
/// Falls back to Haversine (straight-line) distance when OSRM is unreachable.
/// </summary>
public interface IOsrmService
{
    /// <summary>
    /// Returns the driving distance in kilometres between two coordinates.
    /// </summary>
    Task<double> GetRoadDistanceKmAsync(
        double originLat, double originLng,
        double destLat, double destLng,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns both the driving distance (km) and estimated duration (seconds).
    /// </summary>
    Task<(double DistanceKm, double DurationSeconds)> GetRouteAsync(
        double originLat, double originLng,
        double destLat, double destLng,
        CancellationToken cancellationToken = default);
}
