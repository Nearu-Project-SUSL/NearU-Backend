using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services;

/// <summary>
/// Fetches true road-network distance from an OSRM server using its Route API.
/// Falls back to Haversine if OSRM is unreachable and <see cref="OsrmSettings.ThrowOnFailure"/> is false.
///
/// OSRM Route API endpoint used:
///   GET /route/v1/{profile}/{lon1},{lat1};{lon2},{lat2}?overview=false&steps=false
///
/// Public demo server: http://router.project-osrm.org  (rate-limited, for dev only)
/// Self-hosted:        http://your-osrm-host:5000
/// </summary>
public sealed class OsrmService : IOsrmService
{
    private readonly HttpClient _http;
    private readonly OsrmSettings _settings;
    private readonly ILogger<OsrmService> _logger;

    // JSON property paths we care about in the OSRM response:
    //   routes[0].distance  — total distance in metres
    //   routes[0].duration  — total duration in seconds
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OsrmService(
        HttpClient http,
        IOptions<OsrmSettings> settings,
        ILogger<OsrmService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    /// <inheritdoc/>
    public async Task<double> GetRoadDistanceKmAsync(
        double originLat, double originLng,
        double destLat, double destLng,
        CancellationToken cancellationToken = default)
    {
        var (distanceKm, _) = await GetRouteAsync(originLat, originLng, destLat, destLng, cancellationToken);
        return distanceKm;
    }

    /// <inheritdoc/>
    public async Task<(double DistanceKm, double DurationSeconds)> GetRouteAsync(
        double originLat, double originLng,
        double destLat, double destLng,
        CancellationToken cancellationToken = default)
    {
        // OSRM uses longitude,latitude order (GeoJSON convention)
        var url = BuildUrl(originLng, originLat, destLng, destLat);

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = doc.RootElement;

            // OSRM returns { "code": "Ok", "routes": [ { "distance": ..., "duration": ... }, ... ] }
            if (!root.TryGetProperty("code", out var codeEl) ||
                codeEl.GetString() != "Ok")
            {
                var code = root.TryGetProperty("code", out var c) ? c.GetString() : "unknown";
                _logger.LogWarning("OSRM returned non-OK status '{Code}' for route {Url}", code, url);
                return FallbackOrThrow(originLat, originLng, destLat, destLng);
            }

            if (!root.TryGetProperty("routes", out var routesEl) ||
                routesEl.GetArrayLength() == 0)
            {
                _logger.LogWarning("OSRM returned empty routes array for {Url}", url);
                return FallbackOrThrow(originLat, originLng, destLat, destLng);
            }

            var firstRoute = routesEl[0];
            var distanceMetres = firstRoute.GetProperty("distance").GetDouble();
            var durationSecs = firstRoute.GetProperty("duration").GetDouble();

            _logger.LogDebug(
                "OSRM route: {DistM:F1} m / {Dur:F1} s  [{OLat},{OLng} → {DLat},{DLng}]",
                distanceMetres, durationSecs, originLat, originLng, destLat, destLng);

            return (distanceMetres / 1000.0, durationSecs);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout — treat as network failure
            _logger.LogWarning("OSRM request timed out for {Url}", url);
            return FallbackOrThrow(originLat, originLng, destLat, destLng);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "OSRM HTTP request failed for {Url}", url);
            return FallbackOrThrow(originLat, originLng, destLat, destLng);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse OSRM response for {Url}", url);
            return FallbackOrThrow(originLat, originLng, destLat, destLng);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string BuildUrl(double lon1, double lat1, double lon2, double lat2)
    {
        // Format: invariant culture, 6 decimal places (≈ 10 cm precision)
        static string F(double v) => v.ToString("F6", CultureInfo.InvariantCulture);
        var profile = _settings.Profile;
        return $"route/v1/{profile}/{F(lon1)},{F(lat1)};{F(lon2)},{F(lat2)}?overview=false&steps=false";
    }

    private (double DistanceKm, double DurationSeconds) FallbackOrThrow(
        double lat1, double lon1, double lat2, double lon2)
    {
        if (_settings.ThrowOnFailure)
            throw new InvalidOperationException(
                "OSRM routing failed and ThrowOnFailure is enabled. Cannot calculate road distance.");

        _logger.LogWarning(
            "Falling back to Haversine straight-line distance for [{Lat1},{Lon1}] → [{Lat2},{Lon2}]",
            lat1, lon1, lat2, lon2);

        var haversineKm = HaversineKm(lat1, lon1, lat2, lon2);
        // Estimate duration using 30 km/h average speed as a rough fallback
        var estimatedDurationSecs = (haversineKm / 30.0) * 3600.0;
        return (haversineKm, estimatedDurationSecs);
    }

    /// <summary>Haversine great-circle distance in kilometres.</summary>
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * (Math.PI / 180);
}
