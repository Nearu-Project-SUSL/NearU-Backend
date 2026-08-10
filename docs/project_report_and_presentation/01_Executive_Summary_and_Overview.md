# NearU — Hyper-Local Campus Service & Gig Marketplace
## Executive Summary & System Overview

---

## 1. Project Background & Vision

**NearU** is an integrated, hyper-local service discovery, ride-sharing, boarding accommodation, and gig marketplace platform engineered specifically for university campus ecosystems, with tailored primary support for the **Sabaragamuwa University of Sri Lanka (SUSL)** community.

In traditional university setups, student life is severely fragmented. Students face multiple daily friction points:
- Finding suitable boarding places, hostels, or annexes relies on word-of-mouth or physical noticeboards.
- Commuting around campus and nearby towns requires contacting local drivers individually without standard pricing or real-time location tracking.
- Ordering meals, groceries, or customized student gifts is restricted due to lack of a centralized campus directory.
- Finding flexible part-time jobs or freelancing opportunities to support academic living expenses is difficult.
- Campus-adjacent small businesses struggle with digital visibility and targeted marketing toward the student body.

**NearU** unifies these disparate services into a single multi-platform ecosystem comprising:
1. **ASP.NET Core 10 Web API Backend** (PostgreSQL + PostGIS spatial engine, SignalR real-time WebSockets, Redis caching, Firebase Cloud Messaging).
2. **React 18 / Vite Web Frontend** (Tailwind CSS, Material UI, Leaflet Maps, React Router v7).
3. **React Native / Expo Mobile Application** (Expo Router, NativeWind, Native Maps, SecureStore, Expo Location).

---

## 2. Core Feature Domains

| Domain | Key Capabilities | Target Beneficiaries |
| :--- | :--- | :--- |
| **1. Student Accommodation** | Search, filter, and view nearby boarding houses, annexes, hostels. Filter by rent, bed count, gender preference, proximity to university faculties. Direct landlord contact & room item management. | Students, Landlords, Business Owners |
| **2. Campus Ride-Sharing** | On-demand ride hailing connecting students with registered student riders. Instant fare estimations, spatial PostGIS matching, real-time GPS tracking over SignalR, 4-digit OTP ride verification. | Students, Student Riders |
| **3. Campus Dining & Food** | Directory of student canteens, local restaurants, and food shops. Menu item browsing, food ordering, direct store contacting, custom meal additions. | Students, Food Business Owners |
| **4. Campus Marketplace & Services** | Customized gift shop catalog, student photographers portfolio booking, bike rentals, and campus service directory. | Students, Campus Entrepreneurs |
| **5. Student Jobs & Gig Economy** | Hyper-local career hub. Post part-time jobs, campus freelance tasks, or micro-gigs; apply with resume details; track active job listings. | Students, Employers, Local Businesses |
| **6. Campus Transit Guide** | Timetables, route details, fare matrices, and active status tracking for campus buses, Sri Lanka Railway trains, and registered tuk-tuk drivers. | Students, Campus Visitors |
| **7. Student Deals & Offers** | Aggregated student discounts, promotional vouchers, discount codes, and admin-moderated deal campaigns. | Students, Local Businesses |

---

## 3. User Roles & Stakeholder Profiles

NearU enforces strict Role-Based Access Control (RBAC) across four primary user personas:

```
                                  ┌────────────────────────┐
                                  │      System User       │
                                  └───────────┬────────────┘
                                              │
         ┌───────────────────┬────────────────┼───────────────────┐
         ▼                   ▼                ▼                   ▼
┌────────────────┐  ┌────────────────┐  ┌───────────┐   ┌───────────────────┐
│ Student / Guest│  │ Student Rider  │  │  Business │   │ System Admin /    │
│  (Default)     │  │  (Rider Role)  │  │  Owner    │   │ SuperAdmin        │
└────────────────┘  └────────────────┘  └───────────┘   └───────────────────┘
```

1. **Student / Guest (Default Role)**
   - Browse accommodations, marketplace products, food shops, job listings, and transport guides.
   - Request rides with live GPS map tracking and 4-digit OTP security.
   - Post freelance job requests, redeem student deals, write reviews.

2. **Student Rider (Rider Partner)**
   - Toggle Online/Offline availability status.
   - Receive real-time ride request broadcasts over SignalR WebSockets.
   - Accept/decline requests, navigate via interactive maps, verify 4-digit OTP, track daily earnings.

3. **Business Owner / Vendor**
   - Manage business profile, physical location coordinates, contact info.
   - Manage store inventory: food menus, accommodation rooms/items, gift shop products, photography packages.
   - Monitor store analytics, deal submissions, and customer engagement.

4. **System Admin / SuperAdmin**
   - Approve or reject pending student rider applications and business registrations.
   - Moderate user accounts, job postings, deal campaigns, and accommodation listings.
   - Manage global transit schedules (Bus, Train, Tuk-Tuk) and system health.

---

## 4. Multi-Platform System Architecture Overview

```
                                   ┌──────────────────────────────────────────────┐
                                   │              Clients & Devices               │
                                   └──────────────────────┬───────────────────────┘
                                                          │
                    ┌─────────────────────────────────────┴─────────────────────────────────────┐
                    ▼                                                                           ▼
     ┌──────────────────────────────┐                                            ┌──────────────────────────────┐
     │   React 18 / Vite Web App    │                                            │  React Native / Expo Mobile  │
     │  (Port 5173 / Production)    │                                            │  (iOS & Android Native)      │
     └──────────────┬───────────────┘                                            └──────────────┬───────────────┘
                    │                                                                           │
                    └─────────────────────────────────────┬─────────────────────────────────────┘
                                                          │  HTTPS REST API / WSS SignalR
                                                          ▼
                                   ┌──────────────────────────────────────────────┐
                                   │          Nginx Reverse Proxy / TLS           │
                                   └──────────────────────┬───────────────────────┘
                                                          │
                                                          ▼
                                   ┌──────────────────────────────────────────────┐
                                   │           ASP.NET Core 10 Web API            │
                                   │     (Controllers, Services, SignalR Hubs)    │
                                   └──────────────────────┬───────────────────────┘
                                                          │
         ┌────────────────────────────────────────────────┼────────────────────────────────────────────────┐
         ▼                                                ▼                                                ▼
┌─────────────────┐                              ┌─────────────────┐                              ┌─────────────────┐
│   PostgreSQL    │                              │      Redis      │                              │  Firebase Cloud │
│  + PostGIS      │                              │ (Distributed    │                              │    Messaging    │
│ (Spatial DB)    │                              │  Rate Limiting) │                              │ (Web & Mobile)  │
└─────────────────┘                              └─────────────────┘                              └─────────────────┘
```

---

## 5. Key Differentiators & Technical Innovations

1. **Spatial PostGIS Engine**: Leverages NetTopologySuite and PostGIS `geography(Point, 4326)` for sub-meter distance queries, radius searches for accommodations and food outlets, and spatial rider matching.
2. **Dual Real-time Connectivity**: Combines SignalR WebSockets for low-latency live GPS streaming with Firebase Cloud Messaging (FCM) for background mobile push notifications.
3. **Self-Healing Authentication**: Implements JWT Bearer token authentication with automatic background refresh token rotation across both Web Axios interceptors and Mobile SecureStore handlers.
4. **State Machine Driven Rides**: Strict state machine enforcement (`Pending` -> `Accepted` -> `RiderEnRoute` -> `RiderArrived` -> `InProgress` -> `PendingConfirmation` -> `Completed` / `Cancelled`) preventing race conditions during concurrent ride booking.
