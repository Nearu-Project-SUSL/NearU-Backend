# NearU Backend JWT Authentication System

## 📋 Overview

This is a production-ready JWT Authentication system for the NearU Marketplace platform, built with ASP.NET Core .NET 10 Web API. It includes:

- ✅ JWT Access Tokens (short-lived, 15 minutes)
- ✅ Refresh Tokens (long-lived, 7 days) with rotation
- ✅ Role-Based Authorization (Admin, Student, Business, Rider)
- ✅ Secure password hashing with BCrypt
- ✅ Device tracking and IP logging
- ✅ Token revocation and blacklisting
- ✅ Entity Framework Core with Azure SQL
- ✅ Clean architecture with Repository pattern

## 🏗️ Project Structure

```
NearU-Backend.Server/
├── Configuration/
│   └── JwtSettings.cs              # JWT configuration model
├── Controllers/
│   ├── AuthController.cs           # Authentication endpoints
│   └── WeatherForecastController.cs
├── Data/
│   └── NearUDbContext.cs          # Entity Framework DB Context
├── Models/
│   ├── User.cs                     # User & Role entities
│   ├── RefreshToken.cs             # RefreshToken entity
│   └── DTOs/
│       ├── LoginRequest.cs
│       ├── RegisterRequest.cs
│       ├── AuthResponse.cs
│       └── RefreshTokenRequest.cs
├── Repositories/
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IRoleRepository.cs
│   │   └── IRefreshTokenRepository.cs
│   ├── UserRepository.cs
│   ├── RoleRepository.cs
│   └── RefreshTokenRepository.cs
├── Services/
│   ├── Interfaces/
│   │   ├── ITokenService.cs
│   │   └── IAuthService.cs
│   ├── TokenService.cs
│   └── AuthService.cs
├── Program.cs                      # Application configuration
└── appsettings.json               # Configuration file
```

## 🚀 Quick Start

### 1. Update Connection String

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=NearU;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### 2. Update JWT Secret

**IMPORTANT:** Change the JWT Secret in `appsettings.json`:

```json
{
  "JwtSettings": {
    "Secret": "YOUR_VERY_SECURE_SECRET_KEY_MINIMUM_64_CHARACTERS_FOR_PRODUCTION"
  }
}
```

### 3. Run Migrations

```bash
# Add initial migration
dotnet ef migrations add InitialCreate --project NearU-Backend.Server

# Update database
dotnet ef database update --project NearU-Backend.Server
```

### 4. Run the Application

```bash
cd NearU-Backend.Server
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## 📡 API Endpoints

### Public Endpoints (No Authentication Required)

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john_student",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+94771234567",
  "role": "Student"
}
```

**Roles:** `Student`, `Business`, `Rider` (Admin can only be created via database)

**Response:**
```json
{
  "userId": 1,
  "username": "john_student",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+94771234567",
  "role": "Student",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64encodedtoken...",
  "accessTokenExpiration": "2024-01-01T10:15:00Z",
  "refreshTokenExpiration": "2024-01-08T10:00:00Z"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "john_student",
  "password": "SecurePass123!"
}
```

**Response:** Same as Register

#### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "base64encodedtoken..."
}
```

Or send refresh token in cookie (automatic if you used previous endpoints).

#### Revoke Token (Logout)
```http
POST /api/auth/revoke-token
Content-Type: application/json

{
  "refreshToken": "base64encodedtoken..."
}
```

### Protected Endpoints (Authentication Required)

All protected endpoints require the `Authorization` header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Get Current User
```http
GET /api/auth/me
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "id": 1,
  "username": "john_student",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+94771234567",
  "role": "Student",
  "isActive": true,
  "emailConfirmed": false,
  "createdAt": "2024-01-01T10:00:00Z",
  "lastLogin": "2024-01-01T10:00:00Z"
}
```

#### Revoke All Tokens (Logout from All Devices)
```http
POST /api/auth/revoke-all-tokens
Authorization: Bearer {access_token}
```

### Role-Based Endpoints

#### Admin Only
```http
GET /api/auth/admin-only
Authorization: Bearer {admin_access_token}
```

#### Student Only
```http
GET /api/auth/student-only
Authorization: Bearer {student_access_token}
```

#### Business Only
```http
GET /api/auth/business-only
Authorization: Bearer {business_access_token}
```

#### Rider Only
```http
GET /api/auth/rider-only
Authorization: Bearer {rider_access_token}
```

#### Student or Business
```http
GET /api/auth/student-or-business
Authorization: Bearer {student_or_business_access_token}
```

## 🔐 Security Features

### 1. Password Requirements

- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character (@$!%*?&#)

### 2. Token Security

- **Access Token:** Short-lived (15 minutes)
- **Refresh Token:** Long-lived (7 days)
- Tokens use HS256 algorithm
- Refresh tokens are rotated on use
- Old refresh tokens are revoked when new ones are issued

### 3. Refresh Token Features

- Stored in database
- Device tracking (IP, User-Agent, Device Info)
- Automatic expiration
- Manual revocation support
- Replace-on-use pattern

### 4. Database Security

- Passwords hashed with BCrypt
- Unique constraints on username and email
- Refresh tokens indexed for performance
- Cascade delete for user-related data

## 🧪 Testing with React + TypeScript Frontend

### Install Axios

```bash
npm install axios
```

### Create API Service

```typescript
// services/api.ts
import axios from 'axios';

const API_BASE_URL = 'https://localhost:5001/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Important for cookies
});

// Add token to requests
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const response = await axios.post(
          `${API_BASE_URL}/auth/refresh-token`,
          {},
          { withCredentials: true }
        );

        const { accessToken } = response.data;
        localStorage.setItem('accessToken', accessToken);

        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        localStorage.removeItem('accessToken');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;
```

### Auth Service

```typescript
// services/authService.ts
import api from './api';

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  role: 'Student' | 'Business' | 'Rider';
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface AuthResponse {
  userId: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiration: string;
  refreshTokenExpiration: string;
}

export const authService = {
  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await api.post('/auth/register', data);
    const authData = response.data;
    localStorage.setItem('accessToken', authData.accessToken);
    return authData;
  },

  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await api.post('/auth/login', data);
    const authData = response.data;
    localStorage.setItem('accessToken', authData.accessToken);
    return authData;
  },

  async logout(): Promise<void> {
    await api.post('/auth/revoke-token');
    localStorage.removeItem('accessToken');
  },

  async getCurrentUser() {
    const response = await api.get('/auth/me');
    return response.data;
  },
};
```

### Usage in Component

```typescript
// components/Login.tsx
import React, { useState } from 'react';
import { authService } from '../services/authService';

export const Login: React.FC = () => {
  const [credentials, setCredentials] = useState({
    usernameOrEmail: '',
    password: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await authService.login(credentials);
      console.log('Logged in:', response);
      // Redirect to dashboard
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="text"
        placeholder="Username or Email"
        value={credentials.usernameOrEmail}
        onChange={(e) =>
          setCredentials({ ...credentials, usernameOrEmail: e.target.value })
        }
      />
      <input
        type="password"
        placeholder="Password"
        value={credentials.password}
        onChange={(e) =>
          setCredentials({ ...credentials, password: e.target.value })
        }
      />
      <button type="submit">Login</button>
    </form>
  );
};
```

## 🗄️ Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    LastLogin DATETIME2 NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);
```

### Roles Table
```sql
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

INSERT INTO Roles (Name, Description) VALUES
('Admin', 'System Administrator'),
('Student', 'Student User'),
('Business', 'Business User'),
('Rider', 'Delivery Rider');
```

### RefreshTokens Table
```sql
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RevokedAt DATETIME2 NULL,
    ReplacedByToken NVARCHAR(500) NULL,
    ReasonRevoked NVARCHAR(255) NULL,
    CreatedByIp NVARCHAR(45) NOT NULL,
    RevokedByIp NVARCHAR(45) NULL,
    DeviceInfo NVARCHAR(500) NULL,
    UserAgent NVARCHAR(100) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);
```

## 🔧 Configuration

### Environment-Specific Settings

For production, use User Secrets or Azure Key Vault:

```bash
# Set user secrets (Development)
dotnet user-secrets init --project NearU-Backend.Server
dotnet user-secrets set "JwtSettings:Secret" "your-production-secret" --project NearU-Backend.Server
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string" --project NearU-Backend.Server
```

### Azure App Service Configuration

Set these in Application Settings:

- `JwtSettings__Secret`: Your JWT secret
- `ConnectionStrings__DefaultConnection`: Your Azure SQL connection string
- `Cors__AllowedOrigins__0`: Your frontend URL

## 📊 Best Practices Implemented

✅ **Security:**
- BCrypt password hashing
- JWT with HS256
- Secure cookie settings (HttpOnly, Secure, SameSite)
- HTTPS enforcement
- Token expiration and rotation

✅ **Architecture:**
- Clean architecture with separation of concerns
- Repository pattern for data access
- Dependency injection
- Interface-based design

✅ **Database:**
- Entity Framework Core with migrations
- Azure SQL optimized
- Retry logic for transient failures
- Proper indexing

✅ **API Design:**
- RESTful endpoints
- Proper HTTP status codes
- Detailed error messages
- Swagger documentation

✅ **Frontend Compatibility:**
- CORS configured
- Cookie-based refresh tokens
- Axios interceptor support
- TypeScript types included

## 🚀 Deployment to Azure

### 1. Create Azure SQL Database

```bash
az sql server create --name nearuserver --resource-group NearU --location eastus --admin-user adminuser --admin-password YourPassword123!

az sql db create --name NearU --server nearuserver --resource-group NearU --service-objective S0
```

### 2. Create Azure App Service

```bash
az appservice plan create --name NearU-Plan --resource-group NearU --sku B1

az webapp create --name NearU-Backend --resource-group NearU --plan NearU-Plan --runtime "DOTNETCORE|10.0"
```

### 3. Configure App Settings

```bash
az webapp config appsettings set --name NearU-Backend --resource-group NearU --settings \
  "JwtSettings__Secret=your-secret" \
  "ConnectionStrings__DefaultConnection=your-connection-string"
```

### 4. Deploy

```bash
dotnet publish -c Release
az webapp deployment source config-zip --name NearU-Backend --resource-group NearU --src publish.zip
```

## 📝 License

[Specify your license]

## 👥 Contributors

- [Your Team]

## 📞 Support

For issues and questions, contact: support@nearu.com
