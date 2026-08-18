# NearU — Security, Performance & Deployment Specification

---

## 1. Security Architecture & Threat Defense

Security is integrated into every layer of the NearU architecture:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Client Device (Web / Mobile)                       │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ HTTPS / WSS Encrypted TLS 1.3
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│              Nginx Proxy & Distributed IP Rate Limiter                  │
│       (Blocks DDoS, Brute-Force, & Malicious Payload Attacks)           │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Forwarded Headers
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Middleware Pipeline                     │
│  1. JwtBearerAuthentication   --> Decodes & validates JWT Signature     │
│  2. TokenBlacklistMiddleware  --> Checks Redis if JWT jti is revoked    │
│  3. Authorization Middleware  --> Enforces Role-Based Access (RBAC)     │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Parameterized EF Core Queries
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   PostgreSQL Database + BCrypt Hashes                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### **1. JWT Authentication & Refresh Token Rotation:**
- **Short-Lived Access Tokens**: Signed using HMAC-SHA256 with a 15-minute expiration time. Carries claims (`sub`, `email`, `role`, `jti`).
- **Refresh Token Rotation**: Issued upon successful login with a 7-day expiration time. When a refresh request (`POST /api/auth/refresh`) is executed, the current refresh token is revoked, replaced by a newly generated token string, and recorded with an audit trail (`ReplacedByToken`).
- **Self-Healing Interceptors**: Web (Axios interceptor) and Mobile (`api.ts`) automatically catch `401 Unauthorized` responses, enqueue pending requests, fetch new tokens silently, and resume execution without user disruption.

### **2. Token Blacklisting Middleware (`TokenBlacklistMiddleware.cs`):**
- When a user logs out (`POST /api/auth/logout`), the JWT unique identifier (`jti`) is added to Redis cache with a TTL equal to the token's remaining lifespan.
- Every incoming request passes through `TokenBlacklistMiddleware` after authentication. If the token's `jti` exists in the Redis blacklist, the request is immediately rejected with HTTP `401 Unauthorized`.

### **3. Password Security & Hashing:**
- Passwords are never stored in cleartext. Hashing is performed using **BCrypt** (`BCrypt.Net-Next`) with an automatically generated salt:
  ```csharp
  string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
  bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
  ```

### **4. Role-Based Access Control (RBAC):**
- Endpoints enforce authorization policies using `[Authorize(Roles = "...")]` attributes:
  - `Student`: Access to ride hailing, job applications, review submissions.
  - `Rider`: Access to rider console, incoming ride request channels, location streaming.
  - `BusinessOwner`: Access to property management, menu management, store deals.
  - `Admin`, `SuperAdmin`: System-wide moderation, rider approvals, user management.

---

## 2. Performance Optimization & Query Tuning

### **1. PostGIS Spatial Indexing (GIST):**
Spatial point columns (`LastLocation`, `PickupLocation`, `DropoffLocation`) are indexed using PostGIS Generalized Search Tree (GiST) indexes:
```sql
CREATE INDEX idx_rider_statuses_lastlocation 
ON "RiderStatuses" USING GIST ("LastLocation");
```
This enables sub-millisecond distance calculation queries (`ST_DWithin`) across thousands of spatial coordinates.

### **2. EF Core Query Optimizations:**
- **No-Tracking Queries**: Read-only endpoints (e.g., browsing accommodations or menu items) apply `.AsNoTracking()` to eliminate EF Core change tracker overhead.
- **Selective Projection**: API endpoints project explicitly to DTOs (`.Select(a => new AccommodationDto { ... })`), preventing over-fetching of unnecessary database columns.
- **Pagination**: All list endpoints enforce standard limit/offset pagination (`page`, `pageSize`) to bound memory consumption.

### **3. Redis Distributed Caching (`CacheService.cs`):**
Frequently accessed, semi-static data (such as transit timetables for campus buses and trains, food shop metadata, and active deals) are cached in Redis with a configurable TTL, drastically reducing database read pressure.

---

## 3. Deployment & DevOps Infrastructure

### **1. Multi-Stage Dockerfile (`Dockerfile`):**
```dockerfile
# Stage 1: Build & Publish .NET Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NearU_Backend_Revised.csproj", "./"]
RUN dotnet restore "NearU_Backend_Revised.csproj"
COPY . .
RUN dotnet publish "NearU_Backend_Revised.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5059
ENTRYPOINT ["dotnet", "NearU_Backend_Revised.dll"]
```

### **2. Multi-Container Composition (`docker-compose.yml`):**
- Manages three co-located containers:
  1. `nearu-api`: .NET 10 Web API application on port `5059`.
  2. `nearu-db`: PostgreSQL 16 database container with `postgis/postgis` image on port `5432`.
  3. `nearu-redis`: Redis cache server on port `6379`.
- Includes automated health check definitions (`/healthz` endpoint checked every 30 seconds).

### **3. Automated Postman Test Suite & Load Testing:**
- Includes `NearU_Backend.postman_collection.json` containing pre-configured request collections for all 18 controllers.
- Includes `api-load-test.js` (k6 load testing script) for verifying API throughput under concurrent user traffic.
