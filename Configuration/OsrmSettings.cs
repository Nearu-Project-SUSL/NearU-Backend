namespace NearU_Backend_Revised.Configuration;

public class OsrmSettings
{
    /// <summary>
    /// Base URL of the OSRM server, e.g. "http://router.project-osrm.org" (public demo)
    /// or your self-hosted instance "http://localhost:5000".
    /// </summary>
    public string BaseUrl { get; set; } = "http://router.project-osrm.org";

    /// <summary>
    /// Routing profile — "driving" | "walking" | "cycling"
    /// </summary>
    public string Profile { get; set; } = "driving";

    /// <summary>
    /// HTTP timeout in seconds for OSRM requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// When true, any OSRM failure causes an exception instead of falling back to Haversine.
    /// </summary>
    public bool ThrowOnFailure { get; set; } = false;
}
