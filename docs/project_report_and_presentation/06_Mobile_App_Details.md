# NearU — Mobile Application Specification

---

## 1. Expo & React Native Codebase Architecture

The NearU Mobile Application is built using **React Native 0.81.5**, **Expo SDK 54**, **Expo Router v6** (File-based navigation), **NativeWind / Tailwind**, **React Native Maps 1.20.1**, and **Zustand v5**.

```
C:\Users\THIMIRA\NearU-Mobile-App\
├── app/                        # Expo Router File-Based Routing Root
│   ├── (auth)/                 # Authentication Flow Group
│   │   ├── _layout.tsx         # Auth Stack Layout
│   │   ├── login.tsx           # Login Screen with Google OAuth
│   │   ├── register.tsx        # Multi-role Registration Screen
│   │   └── forgot.tsx          # Password Reset Screen
│   ├── (tabs)/                 # Bottom Tab Bar Navigation Group
│   │   ├── _layout.tsx         # Tab Layout with Custom Icons & Badges
│   │   ├── browse.tsx          # Main Discovery & Services Dashboard
│   │   ├── rides.tsx           # Campus Ride-Hailing & Live Map Screen
│   │   ├── favourites.tsx     # Bookmarked Listings Screen
│   │   └── profile.tsx        # User Profile & Account Settings
│   ├── accommodations/         # Accommodations Module
│   │   ├── index.tsx           # Housing List & Filter View
│   │   └── [id].tsx            # Property Detail & Landlord Contact View
│   ├── food/                   # Dining Module
│   │   ├── index.tsx           # Canteens & Food Shop List
│   │   └── [id].tsx            # Shop Menu & Ordering Screen
│   ├── deals/                  # Student Discounts Module
│   │   └── index.tsx           # Student Vouchers List View
│   ├── gifts/                  # Gift Shop Module
│   │   ├── index.tsx           # Gift Shops Directory
│   │   └── [id].tsx            # Custom Gift Items Screen
│   ├── photography/            # Photography Module
│   │   ├── index.tsx           # Student Photographers List
│   │   └── [id].tsx            # Portfolio & Package Booking Screen
│   ├── service/                # Generic Service Route
│   │   └── [id].tsx            # Detailed Service Booking View
│   ├── _layout.tsx             # Master Root Layout & Auth Provider Guard
│   └── index.tsx               # Entry Redirect Handler
├── components/                 # Reusable Native UI Components
│   ├── Button.tsx              # Universal Styled Native Button
│   ├── Card.tsx                # Card Container
│   ├── NearULogo.tsx           # Vector SVG Branding Logo
│   ├── Modal.tsx               # Native Overlay Dialog
│   ├── accommodations/         # Accommodation Cards & Creation Modals
│   ├── rider/                  # Driver Control Sheets & GPS Tracker
│   └── rides/                  # Student Ride Request UI Components
├── hooks/                      # Custom React Hooks
├── services/                   # API Services & Axios Client
│   └── api.ts                  # Axios setup + Expo SecureStore Auth Integration
├── store/                      # Zustand State Stores
└── types/                      # TypeScript Interface Definitions
```

---

## 2. Screen Navigation Map & User Flows

```
                                  ┌───────────────────────────┐
                                  │      app/_layout.tsx      │
                                  │   (Auth Token Guard)      │
                                  └─────────────┬─────────────┘
                                                │
                       ┌────────────────────────┴────────────────────────┐
                       ▼ Unauthenticated                             ▼ Authenticated
        ┌──────────────────────────────┐              ┌──────────────────────────────┐
        │        app/(auth)/           │              │        app/(tabs)/           │
        ├──────────────────────────────┤              ├──────────────────────────────┤
        │  • login.tsx                 │              │  • browse.tsx (Home Hub)     │
        │  • register.tsx              │              │  • rides.tsx (Ride Hailing)  │
        │  • forgot.tsx                │              │  • favourites.tsx            │
        └──────────────────────────────┘              │  • profile.tsx               │
                                                      └──────────────┬───────────────┘
                                                                     │
                                    ┌────────────────────────────────┴────────────────────────────────┐
                                    ▼                                ▼                                ▼
                     ┌──────────────────────────────┐ ┌──────────────────────────────┐ ┌──────────────────────────────┐
                     │    app/accommodations/       │ │          app/food/           │ │         app/deals/           │
                     │  • index.tsx                 │ │  • index.tsx                 │ │  • index.tsx                 │
                     │  • [id].tsx                  │ │  • [id].tsx                  │ └──────────────────────────────┘
                     └──────────────────────────────┘ └──────────────────────────────┘
```

### **Key Mobile User Workflows:**

1. **Authentication Flow (`app/(auth)/`)**:
   - Native email/password authentication or Google One-Tap Sign-In (`@react-native-google-signin/google-signin`).
   - Role-based registration collecting student index numbers, faculty details, or rider vehicle numbers.
   - Tokens securely stored in hardware-backed `Expo SecureStore`.

2. **Campus Ride Hailing (`app/(tabs)/rides.tsx`)**:
   - Embedded native map (`react-native-maps`) displaying passenger pickup location and destination pins.
   - Calculates distance and estimated fare in LKR.
   - Generates 4-digit verification OTP code displayed to student.
   - Real-time GPS marker updates showing driver's approaching vehicle.

3. **Accommodation Finder (`app/accommodations/`)**:
   - Filter nearby hostels and boarding rooms by monthly rent, distance to SUSL campus faculties, and available beds.
   - View property photo carousels, detailed amenity lists, and tap-to-call landlord buttons.

4. **Campus Food & Ordering (`app/food/`)**:
   - Browse nearby student canteens and popular campus dining outlets.
   - View categorized menus (meals, beverages, snacks), select items, and place orders.

---

## 3. Native Device Integration & Capabilities

### **1. High-Accuracy GPS Location (`expo-location`):**
- **Student Location**: Automatically detects device latitude/longitude for pickup location selection.
- **Rider GPS Streaming**: When a student rider toggles status to `Online`, the app initiates background position tracking using `watchPositionAsync`:
  ```typescript
  await Location.watchPositionAsync(
    {
      accuracy: Location.Accuracy.High,
      timeInterval: 3000, // Ping every 3 seconds
      distanceInterval: 5  // Ping every 5 meters
    },
    (location) => {
      sendGpsPingToSignalR(location.coords.latitude, location.coords.longitude);
    }
  );
  ```

### **2. Native Maps & Marker Overlays (`react-native-maps`):**
- Configured with Google Maps API key for Android and Apple Maps for iOS.
- Custom vector marker icons for passenger pickups, dropoffs, and moving vehicle markers.
- Renders route polyline paths between pickup and destination coordinates.

### **3. Hardware Encrypted Storage (`expo-secure-store`):**
- Stores JWT access tokens (`auth_access_token`) and long-lived refresh tokens (`auth_refresh_token`) securely on device keychains (Keychain on iOS, Keystore on Android).
- Eliminates cleartext storage risks associated with standard AsyncStorage.

### **4. Camera & Media Gallery Access (`expo-image-picker`):**
- Enables business owners and landlords to capture photos or upload images from device galleries when creating accommodation listings, food shop menus, or user profile avatars.
