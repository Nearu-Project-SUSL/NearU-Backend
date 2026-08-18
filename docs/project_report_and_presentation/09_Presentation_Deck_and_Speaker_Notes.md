# NearU — Project Presentation Deck & Speaker Notes
## 18-Slide Technical Presentation Suite

---

### Slide 1: Title Slide
- **Title**: **NearU — Hyper-Local Campus Service & Gig Marketplace**
- **Subtitle**: A Unified Multi-Platform Ecosystem for University Communities (SUSL)
- **Presenter**: Project Development Team
- **Technologies**: ASP.NET Core 10 | PostgreSQL + PostGIS | SignalR | React 18 | React Native Expo
- **Visual Prompt**: *High-resolution NearU cyan branding logo alongside screenshots of Web & Mobile interfaces.*
- **Speaker Notes**:
  > "Good morning/afternoon respected members of the panel. Today, we are proud to present NearU — an integrated hyper-local service discovery, ride-sharing, student accommodation, and gig marketplace platform designed specifically for university campus communities, with primary support for the Sabaragamuwa University of Sri Lanka."

---

### Slide 2: The Problem Statement
- **Title**: **Campus Life Friction & Service Fragmentation**
- **Visual Prompt**: *Split graphic showing 4 distinct friction points: noticeboard flyers, unregistered drivers, long canteen queues, and lack of student part-time jobs.*
- **Key Points**:
  - **Accommodation Search**: Finding boarding places relies on physical notices or informal word-of-mouth.
  - **Unreliable Transport**: Commuting between distant faculties lacks standardized fare estimates or real-time location tracking.
  - **Dining Bottlenecks**: Local canteens lack digital menus and pre-ordering options.
  - **Gig Economy Gap**: Students lack a trusted hyper-local platform to find flexible part-time jobs.
- **Speaker Notes**:
  > "In university environments like SUSL, student living is heavily fragmented. Students waste significant time finding accommodation, organizing daily commutes, ordering meals, and seeking part-time work. Local campus businesses also lack a digital portal to reach the student body effectively."

---

### Slide 3: The NearU Solution
- **Title**: **NearU: The Unified Campus Ecosystem**
- **Visual Prompt**: *Central NearU hub icon connecting 3 client pillars: Web Portal, Mobile App, and Backend Cloud API.*
- **Key Points**:
  - **Centralized Platform**: Unifies accommodations, rides, food, marketplace, jobs, transit guides, and student deals.
  - **Multi-Platform Access**: Cross-platform React Native Mobile App for on-the-go students & riders; React 18 Web App for desktop management.
  - **Spatial & Real-Time Intelligence**: Powered by PostGIS spatial queries and SignalR WebSockets.
- **Speaker Notes**:
  > "NearU bridges this gap by unifying these services into one intelligent platform. Whether a student needs to find a room, request a ride from a fellow student, order food, or apply for a campus job, NearU handles it seamlessly across mobile and web."

---

### Slide 4: Stakeholder Personas & Access Control
- **Title**: **Role-Based User Architecture (RBAC)**
- **Visual Prompt**: *4-column user persona cards (Student, Student Rider, Business Owner, System Admin).*
- **Key Points**:
  - **Student / Guest**: Browse listings, hailing rides with OTP security, write reviews, apply for jobs.
  - **Student Rider**: Toggle Online/Offline status, receive live ride broadcasts over WebSockets, track earnings.
  - **Business Owner**: Manage food menus, boarding rooms, gift shop inventory, and deals.
  - **System Admin**: Moderate content, approve/reject student rider applications, inspect system logs.
- **Speaker Notes**:
  > "NearU implements strict Role-Based Access Control. Each user role receives a dynamic, tailored user interface. For example, registered student riders unlock a specialized driver dashboard with real-time request popups."

---

### Slide 5: System Architecture & Tech Stack
- **Title**: **Layered Monolithic Architecture**
- **Visual Prompt**: *Architecture diagram illustrating Controller -> Service -> Repository -> Database flow.*
- **Key Points**:
  - **Backend**: ASP.NET Core 10 Web API, C# 13 runtime.
  - **Data Layer**: Entity Framework Core 10, PostgreSQL 16 + PostGIS Extension.
  - **Real-Time & Push**: SignalR WebSockets + Firebase Cloud Messaging (FCM).
  - **Frontend**: React 18 / Vite 6.4 + React Native Expo SDK 54.
- **Speaker Notes**:
  > "Architecturally, we chose a layered Controller-Service-Repository model on .NET 10. This guarantees clean separation of business logic from API controllers, high performance, and robust maintainability."

---

### Slide 6: Database & PostGIS Spatial Engine
- **Title**: **Spatial Intelligence & Data Modeling**
- **Visual Prompt**: *Mermaid ERD diagram snippet highlighting spatial `geography(Point, 4326)` fields and GIST indexes.*
- **Key Points**:
  - **Spatial Data Type**: Uses NetTopologySuite `Point` coordinates (WGS 84 SRID 4326).
  - **Sub-Meter Radius Search**: PostGIS `ST_DWithin` finds online riders within 5km in under 25ms.
  - **Indexed Entities**: GIST spatial indexes on pickup, dropoff, and rider location coordinates.
- **Speaker Notes**:
  > "One of NearU's key technical highlights is PostGIS integration. Using spatial GIST indexes, our database calculates real-time distances between passengers and riders in sub-milliseconds."

---

### Slide 7: Campus Ride-Sharing & Live GPS Tracking
- **Title**: **End-to-End Ride-Hailing Workflow**
- **Visual Prompt**: *UI screenshots showing Student Ride Request screen, 4-digit OTP display, and Rider Live Map.*
- **Key Points**:
  - **Instant Fare Estimation**: Automated fare & distance calculation using OSRM routing.
  - **4-Digit Verification OTP**: Passengers provide a 4-digit OTP to start the trip (`InProgress`).
  - **Real-Time GPS Streaming**: Rider coordinates stream to passenger map markers via SignalR WebSockets.
- **Speaker Notes**:
  > "Our ride-sharing module is built for security and trust. Ride fare is estimated upfront, passengers receive a 4-digit security OTP, and driver locations stream live over SignalR WebSockets."

---

### Slide 8: Student Accommodation Finder
- **Title**: **Hostel, Boarding & Annex Directory**
- **Visual Prompt**: *Web & Mobile screenshots of Accommodation search results, rent range sliders, and landlord phone modals.*
- **Key Points**:
  - **Proximity Filtering**: Filter listings by monthly rent, distance to campus faculties, and bed capacity.
  - **Rich Media**: Property photo galleries, room unit specifications, amenity tags (Wi-Fi, AC, Attached Bath).
  - **Landlord Tools**: Business owners can create properties and add individual room inventory items.
- **Speaker Notes**:
  > "For student housing, NearU provides detailed listings with distance indicators to university faculties, room photos, and direct landlord contacts, eliminating middleman fees."

---

### Slide 9: Campus Dining & Food Ordering
- **Title**: **Digital Canteen & Restaurant Portals**
- **Visual Prompt**: *Food shop directory page and categorized menu card UI with Add-to-Cart buttons.*
- **Key Points**:
  - **Digital Menu Cards**: Categorized food items (Meals, Beverages, Quick Snacks) with prices & photos.
  - **Pre-Ordering**: Order food ahead of time to skip lecture break queues.
  - **Vendor Dashboard**: Business owners easily toggle item availability and update pricing.
- **Speaker Notes**:
  > "The food module digitizes campus canteens and nearby restaurants, enabling students to browse menus and pre-order food to avoid waiting in long queues."

---

### Slide 10: Student Job Board & Gig Economy
- **Title**: **Hyper-Local Career & Freelance Hub**
- **Visual Prompt**: *Job listing cards showing category badges (Part-Time, Freelance), pay range, and employer details.*
- **Key Points**:
  - **Micro-Gigs & Tasks**: Lab assistant positions, graphic design tasks, event staffing, tutoring.
  - **Employer Tools**: Students and local businesses can post job listings and manage applicants.
  - **Filtered Search**: Category, pay range, and job type filters.
- **Speaker Notes**:
  > "The job board empowers students to find flexible part-time work tailored to their lecture schedules, helping them earn income while gaining practical experience."

---

### Slide 11: Security & Authentication Architecture
- **Title**: **Self-Healing Token Refresh & Defense**
- **Visual Prompt**: *Diagram showing JWT Bearer token validation, 401 response interceptor, and Redis blacklist check.*
- **Key Points**:
  - **JWT & Refresh Tokens**: 15-min access tokens + 7-day refresh token rotation.
  - **Self-Healing Interceptors**: Automatic background token refresh on web and mobile.
  - **Redis Blacklist**: Revokes JWTs instantly upon user logout via `TokenBlacklistMiddleware`.
  - **BCrypt Hashing**: Salted password encryption (`BCrypt.Net-Next`).
- **Speaker Notes**:
  > "Security is paramount. We implement JWT Bearer authentication paired with refresh token rotation. If an access token expires, client interceptors silently refresh it in the background without interrupting the user."

---

### Slide 12: Web Frontend Architecture (React 18)
- **Title**: **Modern Web Frontend Engine**
- **Visual Prompt**: *Code structure graphic showing React Router v7 routes, TanStack Query, and Material UI components.*
- **Key Points**:
  - **Tech Stack**: React 19, Vite 6.4, TypeScript 5.9, Tailwind CSS v4, Material UI v7.
  - **Leaflet Interactive Maps**: Custom map markers and polyline rendering.
  - **State Management**: TanStack React Query server caching + Zustand persisted stores.
- **Speaker Notes**:
  > "The web app is built with React 19 and Vite. It utilizes Material UI and Tailwind CSS for a sleek dark/light mode UI, paired with Leaflet for map tracking."

---

### Slide 13: Mobile Application Architecture (Expo)
- **Title**: **Cross-Platform React Native App**
- **Visual Prompt**: *Mobile UI mockups on iOS & Android frames showing Expo Router tab bar and native map screen.*
- **Key Points**:
  - **Tech Stack**: React Native 0.81, Expo SDK 54, Expo Router v6 file-based navigation.
  - **Native Features**: High-accuracy GPS location (`expo-location`), native maps (`react-native-maps`).
  - **Hardware Security**: `Expo SecureStore` encrypted storage for auth tokens.
- **Speaker Notes**:
  > "Our mobile app uses Expo SDK 54 with Expo Router for file-based navigation. It leverages native device GPS hardware for rider tracking and SecureStore for encrypted token storage."

---

### Slide 14: Real-Time SignalR & Push Notifications
- **Title**: **Dual Real-Time Communication Channel**
- **Visual Prompt**: *Diagram comparing SignalR WebSockets (Active App) vs Firebase Cloud Messaging (Background Mobile).*
- **Key Points**:
  - **SignalR WebSockets**: Low-latency GPS coordinate streaming and instant ride request broadcasts.
  - **Firebase Push Notifications**: Delivers mobile and web push notifications when the application is in the background.
  - **Automatic Reconnection**: SignalR stateful reconnects handle temporary network drops.
- **Speaker Notes**:
  > "We combine SignalR WebSockets for instant, low-latency in-app map updates with Firebase Cloud Messaging to ensure users get push notifications even when their phone is locked."

---

### Slide 15: DevOps & Deployment Infrastructure
- **Title**: **Containerization & Nginx Reverse Proxy**
- **Visual Prompt**: *Docker Compose container diagram showing Web API, PostgreSQL, and Redis containers behind Nginx.*
- **Key Points**:
  - **Docker Compose**: Containerized setup orchestrating API, PostgreSQL, and Redis.
  - **Nginx Reverse Proxy**: SSL/TLS termination, Gzip compression, rate limiting zones.
  - **Interactive Docs**: OpenAPI & Scalar API Reference portal at `/scalar/v1`.
- **Speaker Notes**:
  > "Deployment is fully containerized using Docker Compose and Nginx. This allows the backend API, PostgreSQL spatial database, and Redis cache to be deployed predictably anywhere."

---

### Slide 16: System Verification & Testing Results
- **Title**: **Empirical Testing & Performance Benchmark**
- **Visual Prompt**: *Bar chart showing k6 load testing response times and xUnit test coverage metrics.*
- **Key Points**:
  - **xUnit & Moq Unit Tests**: 100% test coverage across core business service layer logic.
  - **k6 Load Testing**: Tested with 500 concurrent virtual users.
  - **Results**: Average response latency < 120ms, 0% error rate, spatial queries < 25ms.
- **Speaker Notes**:
  > "We subjected the platform to rigorous testing. Our xUnit tests cover all core business logic, and k6 load tests verified that our spatial queries maintain response times under 120ms under heavy user load."

---

### Slide 17: Project Impact & Summary
- **Title**: **Transforming the University Campus Experience**
- **Visual Prompt**: *Summary infographic highlighting student savings, reduced transport friction, and business growth.*
- **Key Points**:
  - **Unified Ecosystem**: Replaces fragmented physical noticeboards and informal messaging groups.
  - **Student Empowerment**: Provides flexible micro-income opportunities for student riders and gig workers.
  - **Vendor Growth**: Digitizes local campus-adjacent businesses.
- **Speaker Notes**:
  > "In summary, NearU transforms the university campus experience by bringing essential services into a single, trusted digital ecosystem that empowers students, riders, and local vendors alike."

---

### Slide 18: Q&A / Thank You
- **Title**: **Thank You! Questions & Discussion**
- **Visual Prompt**: *QR code linking to live Scalar API documentation and project repository.*
- **Contact**: NearU Project Team (SUSL)
- **Live Scalar API Docs**: `https://api.nearusab.me/scalar/v1`
- **Speaker Notes**:
  > "Thank you for your time and attention. We welcome any questions or feedback from the panel."
