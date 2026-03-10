# Testing Guide for NearU JWT Authentication

## 🚀 Step 1: Start the Application

### Apply Database Migration First

```powershell
# Navigate to project directory
cd NearU-Backend.Server

# Apply migration to create database
dotnet ef database update

# Run the application
dotnet run
```

The API should start at:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

---

## 🧪 Step 2: Test Endpoints

### Method 1: Using PowerShell (Recommended for Windows)

#### A. Test Registration

```powershell
# Register a Student
$studentBody = @{
    username = "john_student"
    email = "john@example.com"
    password = "SecurePass123!"
    firstName = "John"
    lastName = "Doe"
    phoneNumber = "+94771234567"
    role = "Student"
} | ConvertTo-Json

$studentResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/register" `
    -Method Post `
    -Body $studentBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

# Save the tokens
$studentAccessToken = $studentResponse.accessToken
$studentRefreshToken = $studentResponse.refreshToken

Write-Host "Student registered successfully!" -ForegroundColor Green
Write-Host "Access Token: $studentAccessToken" -ForegroundColor Cyan
Write-Host "User ID: $($studentResponse.userId)" -ForegroundColor Cyan
```

#### B. Register Users with Different Roles

```powershell
# Register a Business
$businessBody = @{
    username = "jane_business"
    email = "jane@business.com"
    password = "SecurePass123!"
    firstName = "Jane"
    lastName = "Smith"
    phoneNumber = "+94771234568"
    role = "Business"
} | ConvertTo-Json

$businessResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/register" `
    -Method Post `
    -Body $businessBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

$businessAccessToken = $businessResponse.accessToken

# Register a Rider
$riderBody = @{
    username = "mike_rider"
    email = "mike@rider.com"
    password = "SecurePass123!"
    firstName = "Mike"
    lastName = "Johnson"
    phoneNumber = "+94771234569"
    role = "Rider"
} | ConvertTo-Json

$riderResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/register" `
    -Method Post `
    -Body $riderBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

$riderAccessToken = $riderResponse.accessToken

Write-Host "All users registered!" -ForegroundColor Green
```

#### C. Test Login

```powershell
# Login with username
$loginBody = @{
    usernameOrEmail = "john_student"
    password = "SecurePass123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/login" `
    -Method Post `
    -Body $loginBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

Write-Host "Login successful!" -ForegroundColor Green
Write-Host "Access Token: $($loginResponse.accessToken)" -ForegroundColor Cyan

# Or login with email
$loginEmailBody = @{
    usernameOrEmail = "john@example.com"
    password = "SecurePass123!"
} | ConvertTo-Json

$loginEmailResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/login" `
    -Method Post `
    -Body $loginEmailBody `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

#### D. Test Protected Endpoint (Get Current User)

```powershell
# Get current user information
$headers = @{
    Authorization = "Bearer $studentAccessToken"
}

$currentUser = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/me" `
    -Method Get `
    -Headers $headers `
    -SkipCertificateCheck

Write-Host "Current User Info:" -ForegroundColor Yellow
$currentUser | Format-List
```

#### E. Test Role-Based Endpoints

```powershell
# Test Student endpoint (Should work)
$headers = @{ Authorization = "Bearer $studentAccessToken" }

try {
    $studentEndpoint = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/student-only" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
    Write-Host "✓ Student endpoint accessible" -ForegroundColor Green
    Write-Host $studentEndpoint.message
} catch {
    Write-Host "✗ Student endpoint denied" -ForegroundColor Red
}

# Test Business endpoint with Student token (Should fail)
try {
    $businessEndpoint = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/business-only" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
    Write-Host "✗ Unauthorized access granted (This is a bug!)" -ForegroundColor Red
} catch {
    Write-Host "✓ Business endpoint correctly denied for Student" -ForegroundColor Green
}

# Test Business endpoint with Business token (Should work)
$businessHeaders = @{ Authorization = "Bearer $businessAccessToken" }

try {
    $businessEndpoint = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/business-only" `
        -Method Get `
        -Headers $businessHeaders `
        -SkipCertificateCheck
    Write-Host "✓ Business endpoint accessible" -ForegroundColor Green
    Write-Host $businessEndpoint.message
} catch {
    Write-Host "✗ Business endpoint denied" -ForegroundColor Red
}

# Test Rider endpoint with Rider token
$riderHeaders = @{ Authorization = "Bearer $riderAccessToken" }

try {
    $riderEndpoint = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/rider-only" `
        -Method Get `
        -Headers $riderHeaders `
        -SkipCertificateCheck
    Write-Host "✓ Rider endpoint accessible" -ForegroundColor Green
    Write-Host $riderEndpoint.message
} catch {
    Write-Host "✗ Rider endpoint denied" -ForegroundColor Red
}
```

#### F. Test Refresh Token

```powershell
# Wait 2 seconds (simulate some time passing)
Start-Sleep -Seconds 2

# Refresh the access token
$refreshBody = @{
    refreshToken = $studentRefreshToken
} | ConvertTo-Json

$refreshResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/refresh-token" `
    -Method Post `
    -Body $refreshBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

Write-Host "Token refreshed successfully!" -ForegroundColor Green
Write-Host "New Access Token: $($refreshResponse.accessToken)" -ForegroundColor Cyan
Write-Host "New Refresh Token: $($refreshResponse.refreshToken)" -ForegroundColor Cyan

# Update tokens
$studentAccessToken = $refreshResponse.accessToken
$studentRefreshToken = $refreshResponse.refreshToken
```

#### G. Test Logout (Revoke Token)

```powershell
# Revoke the refresh token (logout)
$revokeBody = @{
    refreshToken = $studentRefreshToken
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/revoke-token" `
    -Method Post `
    -Body $revokeBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

Write-Host "Logged out successfully!" -ForegroundColor Green

# Try to use the revoked refresh token (Should fail)
try {
    $failedRefresh = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/refresh-token" `
        -Method Post `
        -Body $revokeBody `
        -ContentType "application/json" `
        -SkipCertificateCheck
    Write-Host "✗ Revoked token still works (This is a bug!)" -ForegroundColor Red
} catch {
    Write-Host "✓ Revoked token correctly rejected" -ForegroundColor Green
}
```

#### H. Test Logout from All Devices

```powershell
# Login again to get new tokens
$loginResponse = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/login" `
    -Method Post `
    -Body $loginBody `
    -ContentType "application/json" `
    -SkipCertificateCheck

$headers = @{ Authorization = "Bearer $($loginResponse.accessToken)" }

# Revoke all tokens for current user
Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/revoke-all-tokens" `
    -Method Post `
    -Headers $headers `
    -SkipCertificateCheck

Write-Host "All tokens revoked successfully!" -ForegroundColor Green
```

---

## Method 2: Using Visual Studio's Built-in HTTP Client

Create a file named `NearU-Backend.Server\AuthTests.http`:

```http
### Variables
@baseUrl = https://localhost:5001
@studentToken = {{register_student.response.body.accessToken}}
@businessToken = {{register_business.response.body.accessToken}}
@refreshToken = {{register_student.response.body.refreshToken}}

### 1. Register Student
# @name register_student
POST {{baseUrl}}/api/auth/register
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

### 2. Register Business
# @name register_business
POST {{baseUrl}}/api/auth/register
Content-Type: application/json

{
  "username": "jane_business",
  "email": "jane@business.com",
  "password": "SecurePass123!",
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "+94771234568",
  "role": "Business"
}

### 3. Register Rider
# @name register_rider
POST {{baseUrl}}/api/auth/register
Content-Type: application/json

{
  "username": "mike_rider",
  "email": "mike@rider.com",
  "password": "SecurePass123!",
  "firstName": "Mike",
  "lastName": "Johnson",
  "phoneNumber": "+94771234569",
  "role": "Rider"
}

### 4. Login with Username
# @name login
POST {{baseUrl}}/api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "john_student",
  "password": "SecurePass123!"
}

### 5. Login with Email
POST {{baseUrl}}/api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "john@example.com",
  "password": "SecurePass123!"
}

### 6. Get Current User
GET {{baseUrl}}/api/auth/me
Authorization: Bearer {{studentToken}}

### 7. Test Student Endpoint
GET {{baseUrl}}/api/auth/student-only
Authorization: Bearer {{studentToken}}

### 8. Test Business Endpoint (Should fail with student token)
GET {{baseUrl}}/api/auth/business-only
Authorization: Bearer {{studentToken}}

### 9. Test Business Endpoint (Should work with business token)
GET {{baseUrl}}/api/auth/business-only
Authorization: Bearer {{businessToken}}

### 10. Test Student or Business Endpoint
GET {{baseUrl}}/api/auth/student-or-business
Authorization: Bearer {{studentToken}}

### 11. Refresh Token
POST {{baseUrl}}/api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "{{refreshToken}}"
}

### 12. Revoke Token (Logout)
POST {{baseUrl}}/api/auth/revoke-token
Content-Type: application/json

{
  "refreshToken": "{{refreshToken}}"
}

### 13. Revoke All Tokens
POST {{baseUrl}}/api/auth/revoke-all-tokens
Authorization: Bearer {{studentToken}}

### 14. Test Invalid Token
GET {{baseUrl}}/api/auth/me
Authorization: Bearer invalid_token_here

### 15. Test Expired Token (wait 16 minutes after login)
GET {{baseUrl}}/api/auth/me
Authorization: Bearer {{studentToken}}
```

---

## Method 3: Using Postman

### Import Collection

1. Open Postman
2. Create new collection: "NearU Auth Tests"
3. Create requests for each endpoint above
4. Use Postman's environment variables for tokens

### Sample Postman Environment:
```json
{
  "name": "NearU Local",
  "values": [
    { "key": "baseUrl", "value": "https://localhost:5001", "enabled": true },
    { "key": "studentToken", "value": "", "enabled": true },
    { "key": "businessToken", "value": "", "enabled": true },
    { "key": "refreshToken", "value": "", "enabled": true }
  ]
}
```

---

## Method 4: Using cURL

### Register
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john_student",
    "email": "john@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+94771234567",
    "role": "Student"
  }' \
  --insecure
```

### Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "john_student",
    "password": "SecurePass123!"
  }' \
  --insecure
```

### Get Current User
```bash
curl -X GET https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE" \
  --insecure
```

---

## 🎯 Complete Test Script (Run All Tests)

Save this as `test-auth.ps1`:

```powershell
Write-Host "🧪 Starting NearU Authentication Tests..." -ForegroundColor Cyan

# Test counter
$passed = 0
$failed = 0

function Test-Endpoint {
    param($Name, $ScriptBlock)
    Write-Host "`n--- Test: $Name ---" -ForegroundColor Yellow
    try {
        & $ScriptBlock
        $script:passed++
        Write-Host "✓ PASSED" -ForegroundColor Green
    } catch {
        $script:failed++
        Write-Host "✗ FAILED: $_" -ForegroundColor Red
    }
}

# Test 1: Register Student
Test-Endpoint "Register Student" {
    $body = @{
        username = "test_student_$(Get-Random)"
        email = "student$(Get-Random)@test.com"
        password = "SecurePass123!"
        firstName = "Test"
        lastName = "Student"
        role = "Student"
    } | ConvertTo-Json

    $script:studentResponse = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/register" `
        -Method Post `
        -Body $body `
        -ContentType "application/json" `
        -SkipCertificateCheck

    if (-not $studentResponse.accessToken) {
        throw "No access token received"
    }
    $script:studentToken = $studentResponse.accessToken
    $script:refreshToken = $studentResponse.refreshToken
}

# Test 2: Register Business
Test-Endpoint "Register Business" {
    $body = @{
        username = "test_business_$(Get-Random)"
        email = "business$(Get-Random)@test.com"
        password = "SecurePass123!"
        firstName = "Test"
        lastName = "Business"
        role = "Business"
    } | ConvertTo-Json

    $script:businessResponse = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/register" `
        -Method Post `
        -Body $body `
        -ContentType "application/json" `
        -SkipCertificateCheck

    $script:businessToken = $businessResponse.accessToken
}

# Test 3: Login
Test-Endpoint "Login with Username" {
    $body = @{
        usernameOrEmail = $studentResponse.username
        password = "SecurePass123!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/login" `
        -Method Post `
        -Body $body `
        -ContentType "application/json" `
        -SkipCertificateCheck

    if (-not $loginResponse.accessToken) {
        throw "Login failed"
    }
}

# Test 4: Get Current User
Test-Endpoint "Get Current User" {
    $headers = @{ Authorization = "Bearer $studentToken" }
    $user = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/me" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck

    if ($user.username -ne $studentResponse.username) {
        throw "Username mismatch"
    }
}

# Test 5: Student Endpoint Access
Test-Endpoint "Student Can Access Student Endpoint" {
    $headers = @{ Authorization = "Bearer $studentToken" }
    $result = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/student-only" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
}

# Test 6: Role-Based Denial
Test-Endpoint "Student Cannot Access Business Endpoint" {
    $headers = @{ Authorization = "Bearer $studentToken" }
    try {
        Invoke-RestMethod `
            -Uri "https://localhost:5001/api/auth/business-only" `
            -Method Get `
            -Headers $headers `
            -SkipCertificateCheck
        throw "Should have been denied"
    } catch {
        if ($_.Exception.Response.StatusCode -eq 403) {
            # Expected 403 Forbidden
        } else {
            throw $_
        }
    }
}

# Test 7: Business Can Access Business Endpoint
Test-Endpoint "Business Can Access Business Endpoint" {
    $headers = @{ Authorization = "Bearer $businessToken" }
    $result = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/business-only" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
}

# Test 8: Refresh Token
Test-Endpoint "Refresh Access Token" {
    $body = @{ refreshToken = $refreshToken } | ConvertTo-Json
    $newTokens = Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/refresh-token" `
        -Method Post `
        -Body $body `
        -ContentType "application/json" `
        -SkipCertificateCheck

    if (-not $newTokens.accessToken) {
        throw "Token refresh failed"
    }
    $script:studentToken = $newTokens.accessToken
    $script:refreshToken = $newTokens.refreshToken
}

# Test 9: Revoke Token
Test-Endpoint "Revoke Refresh Token" {
    $body = @{ refreshToken = $refreshToken } | ConvertTo-Json
    Invoke-RestMethod `
        -Uri "https://localhost:5001/api/auth/revoke-token" `
        -Method Post `
        -Body $body `
        -ContentType "application/json" `
        -SkipCertificateCheck
}

# Test 10: Use Revoked Token (Should Fail)
Test-Endpoint "Revoked Token Cannot Be Used" {
    $body = @{ refreshToken = $refreshToken } | ConvertTo-Json
    try {
        Invoke-RestMethod `
            -Uri "https://localhost:5001/api/auth/refresh-token" `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -SkipCertificateCheck
        throw "Revoked token should not work"
    } catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            # Expected 401 Unauthorized
        } else {
            throw $_
        }
    }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Results:" -ForegroundColor Cyan
Write-Host "  ✓ Passed: $passed" -ForegroundColor Green
Write-Host "  ✗ Failed: $failed" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Cyan

if ($failed -eq 0) {
    Write-Host "`n🎉 All tests passed!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Some tests failed!" -ForegroundColor Yellow
}
```

Run with:
```powershell
.\test-auth.ps1
```

---

## 📊 Expected Test Results

All tests should **PASS** with:
- ✓ Users can register with different roles
- ✓ Users can login with username or email
- ✓ Protected endpoints require valid token
- ✓ Role-based endpoints enforce correct roles
- ✓ Tokens can be refreshed
- ✓ Revoked tokens cannot be reused

---

## 🐛 Common Issues & Solutions

### Issue: "Unable to connect to database"
**Solution:**
```powershell
dotnet ef database update
```

### Issue: "401 Unauthorized"
**Causes:**
- Token expired (15 min lifetime)
- Invalid token format
- Missing "Bearer " prefix

**Solution:**
```powershell
# Ensure format is: "Bearer {token}"
$headers = @{ Authorization = "Bearer $accessToken" }
```

### Issue: "403 Forbidden"
**Cause:** User doesn't have required role

**Solution:** Use correct role token or check role assignment

### Issue: "Certificate validation error"
**Solution:** Use `-SkipCertificateCheck` for local testing

---

## 📝 Next Steps

1. ✅ Run the complete test script
2. ✅ Verify all endpoints work
3. ✅ Test with your React frontend
4. ✅ Add more business-specific endpoints
5. ✅ Deploy to Azure

---

For more information, see:
- `JWT_AUTH_DOCUMENTATION.md` - Complete API docs
- `DATABASE_MIGRATION_GUIDE.md` - Database setup
