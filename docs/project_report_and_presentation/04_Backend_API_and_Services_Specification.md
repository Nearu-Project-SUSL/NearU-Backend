# NearU — Backend API & Services Specification

---

## 1. RESTful API Endpoints Catalogue

All API responses are wrapped in a standard `ApiResponse<T>` envelope:
```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... },
  "errors": null
}
```

---

### **A. Authentication & Security API (`/api/auth`)**

| HTTP Method | Route | Authorization | Request Body / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | `RegisterDto` | Register a new user (Student, Rider, BusinessOwner). Encrypts password using BCrypt. |
| `POST` | `/api/auth/login` | Public | `LoginDto` | Authenticate user credentials. Returns JWT access token (15-min TTL) and refresh token (7-day TTL). |
| `POST` | `/api/auth/google` | Public | `GoogleLoginDto` | Google OAuth token verification via `Google.Apis.Auth`. Auto-provisions user account if new. |
| `POST` | `/api/auth/refresh` | Public | `RefreshTokenDto` | Silent token refresh. Rotates refresh token and invalidates old token string. |
| `POST` | `/api/auth/logout` | Authenticated | `LogoutDto` | Revokes refresh token and adds current JWT `jti` to Redis blacklist. |
| `GET` | `/api/auth/me` | Authenticated | None | Retrieves current authenticated user claims and profile details. |
| `POST` | `/api/auth/forgot-password` | Public | `ForgotPasswordDto` | Generates 6-digit OTP reset code and dispatches password reset email via `EmailService`. |
| `POST` | `/api/auth/reset-password` | Public | `ResetPasswordDto` | Verifies OTP code and updates user password hash. |

---

### **B. Student Accommodation API (`/api/accommodation` & `/api/accommodationitem`)**

| HTTP Method | Route | Authorization | Request / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/accommodation` | Public / Auth | `search`, `minPrice`, `maxPrice` | Fetch accommodations with optional search keywords, price range filtering, and pagination. |
| `GET` | `/api/accommodation/{id}` | Public / Auth | Route `id` | Get detailed accommodation listing including owner info and all available room items. |
| `POST` | `/api/accommodation` | `BusinessOwner`, `Admin` | `CreateAccommodationDto` | Create a new accommodation property listing. |
| `PUT` | `/api/accommodation/{id}` | `BusinessOwner`, `Admin` | `UpdateAccommodationDto` | Update property details (Only property owner or Admin). |
| `DELETE` | `/api/accommodation/{id}` | `BusinessOwner`, `Admin` | Route `id` | Delete accommodation listing (Cascades to room items). |
| `POST` | `/api/accommodationitem` | `BusinessOwner`, `Admin` | `CreateAccommodationItemDto`| Add a new room/bed item to an accommodation property. |
| `PUT` | `/api/accommodationitem/{id}`| `BusinessOwner`, `Admin` | `UpdateAccommodationItemDto`| Update room/bed item pricing, name, or photo URL. |
| `DELETE` | `/api/accommodationitem/{id}`| `BusinessOwner`, `Admin` | Route `id` | Delete a specific room/bed item. |

---

### **C. Campus Ride-Sharing API (`/api/rides`)**

| HTTP Method | Route | Authorization | Request / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/rides/request` | `Student` | `CreateRideRequestDto` | Create a new ride request. Calculates fare & distance via OSRM, generates 4-digit OTP, broadcasts over SignalR. |
| `GET` | `/api/rides/available` | `Rider` | None | Get list of all pending ride requests near the online rider. |
| `POST` | `/api/rides/{id}/accept` | `Rider` | Route `id` | Accept pending ride request. Sets ride state to `Accepted`, notifies student via SignalR & FCM push. |
| `POST` | `/api/rides/{id}/update-location`| `Rider` | `UpdateLocationDto` | Post live GPS coordinates during ride. Streams location to student channel and appends to `TrackingLogs`. |
| `POST` | `/api/rides/{id}/verify-otp` | `Rider` | `VerifyOtpDto` | Verify 4-digit passenger OTP to initiate trip (`InProgress` state). |
| `POST` | `/api/rides/{id}/complete` | `Rider` | Route `id` | Complete active ride. Generates `RideHistory` record, calculates final fare, updates rider stats. |
| `POST` | `/api/rides/{id}/cancel` | Authenticated | `CancelRideDto` | Cancel active ride request. Updates state machine and notifies counterparty. |
| `GET` | `/api/rides/history` | Authenticated | `page`, `pageSize` | Get user ride history (passenger ride history or rider completed trips). |
| `POST` | `/api/rides/device-token` | Authenticated | `DeviceTokenDto` | Register FCM push notification device token for background push delivery. |

---

### **D. Campus Dining & Food API (`/api/foodshop` & `/api/menuitem`)**

| HTTP Method | Route | Authorization | Request / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/foodshop` | Public / Auth | `search`, `category` | List all active food shops & campus canteens. |
| `GET` | `/api/foodshop/{id}` | Public / Auth | Route `id` | Get food shop details and full menu card. |
| `POST` | `/api/foodshop` | `BusinessOwner`, `Admin` | `CreateFoodShopDto` | Register a new canteen or food shop. |
| `POST` | `/api/menuitem` | `BusinessOwner`, `Admin` | `CreateMenuItemDto` | Add a new meal or beverage item to shop menu. |
| `PUT` | `/api/menuitem/{id}` | `BusinessOwner`, `Admin` | `UpdateMenuItemDto` | Edit menu item price, availability toggle, or image. |

---

### **E. Student Job Board API (`/api/job`)**

| HTTP Method | Route | Authorization | Request / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/job` | Public / Auth | `category`, `jobType`, `search`| Browse all job listings with category/type filters. |
| `GET` | `/api/job/{id}` | Public / Auth | Route `id` | Get full job description, company details, pay range. |
| `POST` | `/api/job` | Authenticated | `CreateJobDto` | Post a new student part-time job or freelance task. |
| `PUT` | `/api/job/{id}` | Authenticated | `UpdateJobDto` | Update job post (Only job author or Admin). |
| `DELETE` | `/api/job/{id}` | Authenticated | Route `id` | Delete job listing. |

---

### **F. Admin & Moderation API (`/api/admin` & `/api/usermanagement`)**

| HTTP Method | Route | Authorization | Request / Query | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/admin/pending-riders` | `Admin`, `SuperAdmin` | None | Fetch pending student rider verification applications. |
| `POST` | `/api/admin/approve-rider/{id}`| `Admin`, `SuperAdmin` | Route `id` | Approve rider application (Unlocks `Rider` role). |
| `POST` | `/api/admin/reject-rider/{id}` | `Admin`, `SuperAdmin` | `RejectionDto` | Reject rider application with audit reason. |
| `GET` | `/api/usermanagement/users` | `Admin`, `SuperAdmin` | `search`, `role` | Paginated listing of all registered users. |
| `POST` | `/api/usermanagement/toggle-active/{id}`| `Admin`, `SuperAdmin` | Route `id` | Suspend or reactivate user account. |

---

## 2. SignalR Real-Time WebSockets Specification

**Hub Route:** `wss://{domain}/hubs/rides`

### **Server-to-Client Broadcast Events:**
- `NewRideAvailable(RideRequestDto ride)`: Pushed to `OnlineRiders` group when a new ride is requested.
- `RideStateChanged(int rideId, string newStatus)`: Pushed to `ride:{rideId}` channel on state transition.
- `LocationUpdated(int rideId, double lat, double lng, double heading)`: Live rider GPS coordinates streamed to passenger.
- `NewRiderApplication(RiderApplicationDto app)`: Pushed to `Admins` group when a new rider registers.

---

## 3. Background Services & Scheduled Jobs

NearU executes three background worker tasks (`IHostedService`):

1. **`GhostRiderWorker.cs`**: Periodic background service that monitors active rider heartbeat pings. If an online rider fails to send a GPS location ping for 5 minutes, their status is set to `Offline` to prevent stale dispatching.
2. **`GhostRiderCleanupWorker.cs`**: Cleans up abandoned ghost tracking logs older than 30 days from `TrackingLogs` table to maintain spatial query performance.
3. **`RideLifecycleWorker.cs`**: Automated cleanup worker that cancels pending ride requests that remain unaccepted for longer than 15 minutes, sending push notifications to affected students.
