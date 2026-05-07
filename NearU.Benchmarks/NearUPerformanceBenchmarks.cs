using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

// ─────────────────────────────────────────────────────────────────────────────
// Entry Point
// Must run in Release mode: dotnet run -c Release
// ─────────────────────────────────────────────────────────────────────────────
BenchmarkRunner.Run<NearUPerformanceBenchmarks>();

// ─────────────────────────────────────────────────────────────────────────────
// NearU Performance Benchmarks
//
// Covers the three hottest paths in the NearU API:
//   1. JWT Access Token generation   → hits every authenticated request
//   2. BCrypt password verification  → hits every login attempt
//   3. JSON serialisation of lists   → hits every GET /accommodations etc.
//   4. LINQ filtering simulation     → represents in-memory search queries
//
// [MemoryDiagnoser]  → tracks heap allocations (important for SCI/GC pressure)
// [JsonExporterAttribute.Full] → writes the JSON file read by the CI action
// ─────────────────────────────────────────────────────────────────────────────
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
[MarkdownExporter]
public class NearUPerformanceBenchmarks
{
    // ── Shared state set up once per benchmark class ──────────────────────────
    private const string JwtSecret = "NearU-SuperSecret-BenchmarkKey-32+chars!!";
    private const string JwtIssuer = "https://api.nearusab.me";
    private const string JwtAudience = "nearu-mobile-app";

    private string _bcryptHash = null!;
    private List<AccommodationDto> _accommodations = null!;

    // ── Setup — runs ONCE before all benchmarks ───────────────────────────────
    [GlobalSetup]
    public void Setup()
    {
        // Pre-compute a valid BCrypt hash so we measure *verification* speed,
        // not hashing speed (verification is the bottleneck on login).
        _bcryptHash = BCrypt.Net.BCrypt.HashPassword("StudentPassword123!", workFactor: 11);

        // Seed a realistic accommodation list (50 items ≈ a full page response)
        _accommodations = Enumerable.Range(1, 50).Select(i => new AccommodationDto
        {
            Id       = i,
            Name     = $"Near-Campus House {i}",
            Address  = $"{i} University Road, Sabaragamuwa",
            Rent     = 8000 + (i * 500),
            Rooms    = (i % 3) + 1,
            IsActive = i % 5 != 0   // ~20% inactive
        }).ToList();
    }

    // ── Benchmark 1: JWT Token Generation ─────────────────────────────────────
    // Every protected API call needs a valid JWT. This is called on every login
    // and every token refresh — understanding its cost matters.
    [Benchmark(Description = "Generate JWT Access Token")]
    public string GenerateJwtToken()
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   "user-benchmark-id"),
            new Claim(JwtRegisteredClaimNames.Email, "student@sabaragamuwa.ac.lk"),
            new Claim(ClaimTypes.Role,               "Student"),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             JwtIssuer,
            audience:           JwtAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Benchmark 2: BCrypt Password Verification ──────────────────────────────
    // BCrypt is intentionally slow. Measuring its cost confirms the work-factor
    // (11) is acceptable for our user-facing login latency SLA.
    [Benchmark(Description = "BCrypt Password Verification (workFactor=11)")]
    public bool VerifyPassword()
    {
        return BCrypt.Net.BCrypt.Verify("StudentPassword123!", _bcryptHash);
    }

    // ── Benchmark 3: JSON Serialisation — Accommodation List ──────────────────
    // Every GET /api/accommodations call serialises this list. Measures the raw
    // throughput of System.Text.Json on realistic NearU payload shapes.
    [Benchmark(Description = "Serialise 50 Accommodations to JSON")]
    public string SerialiseAccommodationList()
    {
        return JsonSerializer.Serialize(_accommodations);
    }

    // ── Benchmark 4: LINQ Active-Only Filter ──────────────────────────────────
    // Simulates the most common repository-side filter: exclude inactive
    // listings. Tracks allocation cost of LINQ materialisation.
    [Benchmark(Description = "Filter Active Accommodations (LINQ)")]
    public List<AccommodationDto> FilterActiveAccommodations()
    {
        return _accommodations
            .Where(a => a.IsActive)
            .OrderBy(a => a.Rent)
            .ToList();
    }

    // ── Benchmark 5: JSON Deserialisation ─────────────────────────────────────
    // Measures the cost of parsing an incoming JSON body — relevant for POST
    // and PUT endpoints that accept accommodation or job payloads.
    [Benchmark(Description = "Deserialise Accommodation JSON Body")]
    public AccommodationDto? DeserialiseAccommodation()
    {
        var json = """
            {
                "id": 1,
                "name": "Sunset Hostel",
                "address": "45 Ratnapura Road, Belihuloya",
                "rent": 9500,
                "rooms": 2,
                "isActive": true
            }
            """;

        return JsonSerializer.Deserialize<AccommodationDto>(json);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Custom BenchmarkDotNet Config
// Uses ShortRun in CI (fewer iterations = faster pipeline) while keeping
// the full FullRun available for local profiling via env variable override.
// ─────────────────────────────────────────────────────────────────────────────
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        // In CI we set BENCHMARK_JOB=short to keep pipeline time under 5 min.
        var isCi = Environment.GetEnvironmentVariable("BENCHMARK_JOB")?.Equals(
            "short", StringComparison.OrdinalIgnoreCase) == true;

        AddJob(isCi ? Job.ShortRun : Job.Default);

        // Always export both formats:
        //   • JSON  → consumed by benchmark-action/github-action-benchmark
        //   • Markdown → attached as PR comment / artifact for humans
        AddExporter(JsonExporter.Full);
        AddExporter(MarkdownExporter.Default);

        // Memory diagnoser so we see Gen0/Gen1/Gen2 GC counts + allocated bytes
        AddDiagnoser(MemoryDiagnoser.Default);

        AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Lightweight DTO used by benchmarks — mirrors the real Accommodation shape
// without taking a dependency on the EF DbContext.
// ─────────────────────────────────────────────────────────────────────────────
public record AccommodationDto
{
    public int    Id       { get; init; }
    public string Name     { get; init; } = string.Empty;
    public string Address  { get; init; } = string.Empty;
    public decimal Rent    { get; init; }
    public int    Rooms    { get; init; }
    public bool   IsActive { get; init; }
}
