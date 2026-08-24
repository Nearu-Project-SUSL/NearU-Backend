# NearU — System Architecture & Technology Stack Specification

---

## 1. Architectural Blueprint & Design Patterns

The NearU backend is engineered using a robust, decoupled **Layered Monolithic Architecture** following the **Controller-Service-Repository-DTO** pattern. This architecture ensures high maintainability, strict separation of concerns, testability, and seamless scalability.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      HTTP Requests & WebSockets                         │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           API Controller Layer                          │
│   (Input Validation, Route Handling, Response Wrapping, Authorization)  │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ DTOs (Data Transfer Objects)
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           Business Service Layer                        │
│   (Business Rules, OSRM Routing, Ride State Machine, FCM Notifications)  │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Domain Models & Entities
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            Repository Layer                             │
│       (EF Core Data Abstraction, Generic Repository, Custom Queries)    │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Entity Framework Core 10
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       PostgreSQL Database + PostGIS                     │
└─────────────────────────────────────────────────────────────────────────┘
```

### **Core Design Principles:**
1. **Controller-Service-Repository Pattern**: Controllers remain thin, delegating all domain logic to business services (`RideService`, `FoodShopService`, `JobService`). Services interact with EF Core repositories or `ApplicationDbContext`.
2. **DTO Abstraction**: Raw database entities are never exposed directly via API endpoints. Every request/response uses strong typed DTOs (e.g., `RegisterDto`, `CreateRideRequestDto`, `AccommodationResponseDto`).
3. **Dependency Injection**: Services and repositories are registered with `Scoped` lifetime in `Program.cs` (`builder.Services.AddScoped<IRideService, RideService>()`).
4. **Self-Healing Token Refresh**: Interceptors automatically catch HTTP `401 Unauthorized` responses and initiate background token refreshes (`/api/auth/refresh`).
5. **Stateful WebSocket Channels**: SignalR Hubs (`/hubs/rides`) handle connection multiplexing, automatic reconnects (`AllowStatefulReconnects = true`), and geographic group joins.

---

## 2. Complete Technology Stack Matrix

### **A. Backend Ecosystem**
| Component | Technology | Version | Purpose / Description |
| :--- | :--- | :--- | :--- |
| **Runtime Framework** | .NET Web API | `net10.0` | High-performance C# 13 web runtime. |
| **ORM / Data Access** | Entity Framework Core | `10.0.5` | Code-First migrations, LINQ queries, eager loading (`Include`). |
| **Primary Database** | PostgreSQL | `16.x` / `10.0.1` | ACID-compliant relational storage via `Npgsql.EntityFrameworkCore.PostgreSQL`. |
| **Spatial Engine** | PostGIS + NetTopologySuite | `2.6.0` | Spatial geometry data type (`geography(Point, 4326)`), distance calculation algorithms. |
| **Real-time WebSockets**| ASP.NET Core SignalR | `10.0.3` | Dual WebSocket channels for live ride GPS updates & status broadcasting. |
| **Distributed Cache** | Redis | `10.0.7` | Session token blacklisting (`TokenBlacklistMiddleware`), rate limiting, and SignalR scale-out. |
| **Push Notifications** | Firebase Admin SDK | `3.5.0` | Server-side FCM token messaging to mobile and web devices. |
| **Security & Auth** | JWT Bearer & BCrypt | `8.3.1` / `4.1.0` | JWT validation, SHA-256 / BCrypt password hashing, refresh token rotation. |
| **Rate Limiting** | AspNetCoreRateLimit | `5.0.0` | IP-based distributed rate limiting for API DDoS defense. |
| **API Documentation** | Scalar API Reference | `2.16.2` | Interactive OpenAPI documentation portal (`/scalar/v1`). |
| **Reverse Proxy** | Nginx | Stable | Production reverse proxy, SSL termination, static file compression. |
| **Containerization** | Docker & Compose | Multi-Stage | Isolated container runtime for API, PostgreSQL, and Redis. |

### **B. Web Frontend Ecosystem**
| Component | Technology | Version | Purpose / Description |
| :--- | :--- | :--- | :--- |
| **UI Library** | React | `19.1.0` | Component-driven declarative web interface. |
| **Build Tooling** | Vite | `6.4.x` | ESM fast bundler, HMR, path alias resolution (`@/*`). |
| **Language** | TypeScript | `5.9.2` | Strict type checking across components, services, and state models. |
| **Routing** | React Router | `v7.13.0` | Data router with `createBrowserRouter`, route guards, lazy chunk retries. |
| **Styling** | Tailwind CSS v4 + MUI | `v4.1` / `v7.3` | Custom utility classes + Material UI enterprise component library (`#2E9EBF` primary accent). |
| **Primitives** | Radix UI | Latest | Accessible UI components (Dialog, Accordion, Popover, Select, Tabs). |
| **Animations** | Motion / Framer Motion | `12.23` | GPU-accelerated page transitions and interactive micro-animations. |
| **Interactive Maps** | Leaflet & React-Leaflet | `1.9.4` / `4.2.1` | Web maps for pickup/dropoff selection and rider marker tracking. |
| **Server State** | TanStack React Query | `v5.99` | Automatic caching, stale-time management, background refetching. |
| **Client State** | Zustand | `v5.0` | Persisted store for notification drawer and rider operational state machine. |
| **HTTP Client** | Axios | `1.13.6` | Intercepted client with automatic token injection and 401 token refresh queue. |

### **C. Mobile Application Ecosystem**
| Component | Technology | Version | Purpose / Description |
| :--- | :--- | :--- | :--- |
| **Mobile Framework** | React Native | `0.81.5` | Cross-platform native mobile application engine. |
| **Tooling & Build** | Expo SDK | `~54.0.35` | Expo managed workflow, development client (`expo-dev-client`), EAS build pipeline. |
| **Navigation** | Expo Router | `~6.0.24` | File-based routing system (`app/(auth)`, `app/(tabs)`). |
| **Styling** | NativeWind / Tailwind | Latest | Universal Tailwind CSS styling for React Native components. |
| **Native Maps** | React Native Maps | `1.20.1` | Native iOS MapKit & Android Google Maps with custom markers and polylines. |
| **Location Services** | Expo Location | `~18.0.x` | High-accuracy device GPS coordinate tracking (`watchPositionAsync`). |
| **Secure Storage** | Expo SecureStore | `~15.0.8` | Encrypted hardware keychain storage for JWT access & refresh tokens. |
| **Media Handling** | Expo Image Picker | `~17.0.11` | Access device camera and photo gallery for image upload listings. |
| **Google Sign-In** | Google Sign-In Native | `^16.1.2` | Native OAuth integration for Android & iOS. |

---

## 3. Infrastructure & Deployment Architecture

```
                             ┌────────────────────────────────────────┐
                             │              Internet                  │
                             └───────────────────┬────────────────────┘
                                                 │
                                                 ▼
                             ┌────────────────────────────────────────┐
                             │              Nginx Proxy               │
                             │  (Port 80/443 -> SSL Termination)     │
                             └───────────────────┬────────────────────┘
                                                 │
                  ┌──────────────────────────────┴──────────────────────────────┐
                  ▼                                                             ▼
┌──────────────────────────────────────────┐                 ┌──────────────────────────────────────────┐
│      Docker Container: NearU Web API     │                 │     Docker Container: NearU Web API      │
│       (.NET 10 Kestrel Port 5059)        │                 │       (.NET 10 Kestrel Port 5059)        │
└─────────────────┬────────────────────────┘                 └─────────────────┬────────────────────────┘
                  │                                                            │
                  └──────────────────────────────┬─────────────────────────────┘
                                                 │
        ┌────────────────────────────────────────┼────────────────────────────────────────┐
        ▼                                        ▼                                        ▼
┌──────────────────────────────┐ ┌──────────────────────────────┐ ┌──────────────────────────────┐
│  Container: PostgreSQL 16    │ │    Container: Redis Cache    │ │   External: Firebase Cloud   │
│       (+ PostGIS Extension)  │ │   (Port 6379 Rate-Limiter)   │ │           Messaging          │
└──────────────────────────────┘ └──────────────────────────────┘ └──────────────────────────────┘
```

### **Nginx Reverse Proxy Configuration (`nginx.conf` Highlights):**
- **Upstream Pool**: Load balances requests across Web API instances.
- **WebSocket Upgrade**: Handles `Upgrade` and `Connection "Upgrade"` headers for SignalR `/hubs/rides`.
- **Gzip Compression**: Compresses text, JSON, SVG, and CSS payloads over 1KB.
- **Rate Limiting Zones**: Protects `/api/auth/login` and `/api/auth/register` endpoints against brute-force attacks.
