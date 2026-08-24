# NearU — Web Frontend Application Specification

---

## 1. Directory Structure & Codebase Architecture

The NearU Web Frontend is built with **React 19**, **Vite 6.4**, **TypeScript 5.9**, **Tailwind CSS v4**, and **React Router v7**.

```
C:\Users\THIMIRA\NearU-Frontend\src\
├── api/                    # REST API Client Services & Dual Axios Instance Setup
│   ├── axios.ts            # defaultAxios vs axiosPrivate + 401 Interceptors
│   ├── accommodationService.ts
│   ├── adminService.ts
│   ├── authService.ts
│   ├── businessService.ts
│   ├── foodapi.ts
│   ├── jobService.ts
│   ├── riderService.ts
│   └── Ridesapi.ts
├── app/
│   ├── components/         # Domain-Specific UI Components
│   │   ├── accommodation/  # Add/Edit Accommodation & Room modals
│   │   ├── dashboard/      # ServiceCard, DealCard, TestimonialCard
│   │   ├── food/           # Shop & Menu cards, CRUD dialogs
│   │   ├── layout/         # Navbar, Sidebar, NotificationDropdown, PageLayout
│   │   ├── ride/           # Student ride screens (Request, Pending, In-Progress)
│   │   └── rider/          # Driver UI (MapView, AnimatedMarker, RideRequestSheet)
│   ├── context/            # React Context Providers
│   │   ├── AuthContext.tsx # User auth state, startup token refresh, FCM trigger
│   │   ├── SidebarContext.tsx # Sidebar expanded/collapsed state
│   │   └── ThemeContext.tsx # Light/Dark mode switcher
│   ├── hooks/              # Custom Hooks
│   │   ├── useAccommodation.ts
│   │   ├── useAuth.ts
│   │   ├── useGeolocation.ts # Browser GPS Location hook
│   │   ├── useJobs.ts
│   │   ├── useRideHub.ts   # SignalR hub connection manager
│   │   └── useStudentRideHub.ts
│   ├── pages/              # View Page Components
│   │   ├── protected/      # Role-Guarded Pages
│   │   │   ├── AdminHome.tsx
│   │   │   ├── BusinessOwnerHome.tsx
│   │   │   ├── Food.tsx
│   │   │   ├── Home.tsx
│   │   │   ├── Jobs.tsx
│   │   │   ├── Profile.tsx
│   │   │   ├── RiderHome.tsx
│   │   │   ├── Ridespage.tsx
│   │   │   └── transport/
│   │   └── public/         # Public Pages
│   │       ├── Accommodation.tsx
│   │       ├── LandingPage.tsx
│   │       ├── Login.tsx
│   │       └── Register.tsx
│   ├── routing/
│   │   └── ProtectedRoute.tsx # Route Guard checking roles & login status
│   ├── services/
│   │   ├── firebaseService.ts # FCM Initialization
│   │   └── rideHubService.ts  # SignalR connection singleton
│   ├── store/
│   │   ├── notificationStore.ts # Zustand store for notification drawer
│   │   └── riderStore.ts        # Zustand state machine for rider driver flow
│   └── routes.tsx          # React Router v7 browser routes configuration
```

---

## 2. Route Matrix & Page Catalog

| Path | Access Level | Component | Key Responsibilities & Capabilities |
| :--- | :--- | :--- | :--- |
| `/` | Public | `LandingPage.tsx` | Hero section, service feature showcase, deals preview, testimonials, call-to-action buttons. |
| `/login` | Public | `Login.tsx` | User login form, Google OAuth button, password toggle, link to registration. |
| `/register` | Public | `Register.tsx` | Multi-step registration flow. Select role (Student, Rider, BusinessOwner) with role-specific fields. |
| `/home` | Protected | `Home.tsx` | Student Home Dashboard. Quick action shortcuts, active service metrics, top student deals, nearby food. |
| `/rides` | `Student` | `Ridespage.tsx` | Interactive Leaflet map ride hailing interface. Pick location, estimate fare, broadcast ride request, view 4-digit OTP, track rider in real-time. |
| `/rider-home` | `Rider` | `RiderHome.tsx` | Driver portal. Toggle Online/Offline status, receive real-time request popups over SignalR, accept/decline, verify OTP, stream GPS position. |
| `/food` | Protected | `Food.tsx` | Canteen and restaurant directory. Browse shops, filter menus, place orders, contact shop owners. |
| `/accommodation` | Public / Auth | `Accommodation.tsx`| Student housing finder. Filter by rent range, boarding type, distance to campus. View room galleries and contact landlords. |
| `/jobs` | Protected | `Jobs.tsx` | Student job marketplace. Browse part-time jobs, search by category, post new job listings, edit or delete owned posts. |
| `/transport` | Protected | `Transport.tsx` | Campus transit schedule guide for buses, trains, and tuk-tuk drivers. |
| `/deals` | Protected | `Deals.tsx` | Student discount voucher catalog. Filter by category, copy promo codes. |
| `/profile` | Protected | `Profile.tsx` | Account management. Edit profile info, upload avatar, change password, switch theme mode. |
| `/admin-home` | `Admin` | `AdminHome.tsx` | System admin console. Review/approve pending rider applications, manage users, monitor platform stats. |
| `/business-owner-home`| `BusinessOwner`| `BusinessOwnerHome.tsx`| Vendor portal. Manage shop listings, food menus, accommodation rooms, and deal submissions. |

---

## 3. Design System & UX Highlights

### **1. Signature Aesthetics & Theme Matrix:**
- **Primary Brand Accent**: NearU Cyan (`#2E9EBF`).
- **Dark/Light Mode**: Synced across Tailwind CSS classes and MUI `createTheme` wrapper. Managed by `ThemeContext.tsx` and persisted in `localStorage`. Applies `data-theme` attribute to root `<html>` element.

### **2. Layout Architecture:**
- **Glassmorphic Top Navbar (`Navbar.tsx`)**: Sticky header featuring `backdrop-filter: blur(20px)`, logo, notification dropdown badge (`notificationStore`), theme toggle, and user avatar.
- **Collapsible Responsive Sidebar (`Sidebar.tsx`)**:
  - Desktop: Collapses between expanded (`252px`) and mini-variant (`68px`).
  - Mobile: Full sliding `Drawer` triggered via hamburger menu.
  - Role Awareness: Dynamically renders relevant menu items depending on user permissions.

### **3. Interactive Map & GPS Tracking (`MapView.tsx` & `AnimatedMarker.tsx`):**
- Integrated with Leaflet maps (`leaflet` & `react-leaflet`).
- Custom marker rendering for pickup (green pin), dropoff (red pin), and moving rider vehicle (animated icon).
- Smooth position interpolation when receiving new GPS coordinate pings over SignalR.

---

## 4. State Management & API Integration Architecture

```
                                  ┌───────────────────────────┐
                                  │      React Component      │
                                  └─────────────┬─────────────┘
                                                │
                       ┌────────────────────────┴────────────────────────┐
                       ▼                                                 ▼
        ┌─────────────────────────────┐                   ┌─────────────────────────────┐
        │   TanStack React Query v5   │                   │      Zustand Store v5       │
        │   (Server-State Caching)    │                   │   (Client UI & Notifications│
        └──────────────┬──────────────┘                   └─────────────────────────────┘
                       │
                       ▼
        ┌─────────────────────────────┐
        │   Axios Private Client      │
        │  (Authorization Interceptor)│
        └──────────────┬──────────────┘
                       │
       ┌───────────────┴───────────────┐
       ▼ 401 Unauthorized             ▼ Success
┌──────────────────────────────┐ ┌──────────────────────────────┐
│  Automatic Token Refresh     │ │     Return Response Data     │
│ (/api/auth/refresh Endpoint) │ └──────────────────────────────┘
└──────────────────────────────┘
```

1. **Dual Axios Setup**:
   - `defaultAxios`: Used for public endpoints and token refresh calls.
   - `axiosPrivate`: Injects `Authorization: Bearer <token>` into requests.
2. **Response Interceptor Queue**: When a `401 Unauthorized` response is caught, subsequent requests are queued while a single POST to `/api/auth/refresh` is made. Upon success, queued requests retry with the new token.
3. **SignalR Connection Singleton (`rideHubService.ts`)**: Reuses WebSocket connection across components, subscribing to real-time events (`NewRideAvailable`, `RideStateChanged`, `LocationUpdated`).
