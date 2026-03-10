#!/usr/bin/env pwsh
# NearU Backend Authentication Test Script
# Run with: .\test-auth.ps1

param(
    [string]$BaseUrl = "https://localhost:5001",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║          NearU Backend - Authentication Test Suite          ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Test counters
$script:passed = 0
$script:failed = 0
$script:total = 0

# Store tokens
$script:tokens = @{}

function Write-TestHeader {
    param([string]$Text)
    Write-Host "`n$('═' * 70)" -ForegroundColor DarkGray
    Write-Host "  $Text" -ForegroundColor Yellow
    Write-Host "$('═' * 70)" -ForegroundColor DarkGray
}

function Write-TestName {
    param([string]$Name)
    $script:total++
    Write-Host "`n[$script:total] " -NoNewline -ForegroundColor Cyan
    Write-Host $Name -NoNewline
}

function Write-TestResult {
    param([bool]$Success, [string]$Message = "")
    if ($Success) {
        Write-Host " ✓ PASSED" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host " ✗ FAILED" -ForegroundColor Red
        $script:failed++
        if ($Message) {
            Write-Host "   Error: $Message" -ForegroundColor Red
        }
    }
}

function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = $null,
        [int]$ExpectedStatus = 200
    )

    $uri = "$BaseUrl$Endpoint"
    $headers = @{ "Content-Type" = "application/json" }
    
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    try {
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $headers
            SkipCertificateCheck = $true
        }

        if ($Body) {
            $params["Body"] = ($Body | ConvertTo-Json -Depth 10)
        }

        if ($Verbose) {
            Write-Host "  → $Method $uri" -ForegroundColor DarkGray
        }

        $response = Invoke-RestMethod @params
        
        return @{
            Success = $true
            Data = $response
            StatusCode = 200
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        
        if ($statusCode -eq $ExpectedStatus) {
            return @{
                Success = $true
                StatusCode = $statusCode
            }
        }

        return @{
            Success = $false
            Error = $_.Exception.Message
            StatusCode = $statusCode
        }
    }
}

# ============================================================================
# REGISTRATION TESTS
# ============================================================================

Write-TestHeader "REGISTRATION TESTS"

# Test 1: Register Student
Write-TestName "Register Student User"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/register" -Body @{
    username = "test_student_$(Get-Random -Maximum 9999)"
    email = "student$(Get-Random -Maximum 9999)@test.com"
    password = "SecurePass123!"
    firstName = "John"
    lastName = "Doe"
    phoneNumber = "+94771234567"
    role = "Student"
}

if ($result.Success) {
    $script:tokens.Student = $result.Data.accessToken
    $script:tokens.StudentRefresh = $result.Data.refreshToken
    $script:studentUsername = $result.Data.username
    $script:studentEmail = $result.Data.email
    Write-TestResult -Success $true
} else {
    Write-TestResult -Success $false -Message $result.Error
}

# Test 2: Register Business
Write-TestName "Register Business User"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/register" -Body @{
    username = "test_business_$(Get-Random -Maximum 9999)"
    email = "business$(Get-Random -Maximum 9999)@test.com"
    password = "SecurePass123!"
    firstName = "Jane"
    lastName = "Smith"
    phoneNumber = "+94771234568"
    role = "Business"
}

if ($result.Success) {
    $script:tokens.Business = $result.Data.accessToken
    Write-TestResult -Success $true
} else {
    Write-TestResult -Success $false -Message $result.Error
}

# Test 3: Register Rider
Write-TestName "Register Rider User"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/register" -Body @{
    username = "test_rider_$(Get-Random -Maximum 9999)"
    email = "rider$(Get-Random -Maximum 9999)@test.com"
    password = "SecurePass123!"
    firstName = "Mike"
    lastName = "Johnson"
    phoneNumber = "+94771234569"
    role = "Rider"
}

if ($result.Success) {
    $script:tokens.Rider = $result.Data.accessToken
    Write-TestResult -Success $true
} else {
    Write-TestResult -Success $false -Message $result.Error
}

# Test 4: Duplicate Username (Should Fail)
Write-TestName "Reject Duplicate Username"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/register" -ExpectedStatus 400 -Body @{
    username = $studentUsername
    email = "different$(Get-Random)@test.com"
    password = "SecurePass123!"
    firstName = "Test"
    lastName = "User"
    role = "Student"
}
Write-TestResult -Success ($result.StatusCode -eq 400)

# Test 5: Weak Password (Should Fail)
Write-TestName "Reject Weak Password"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/register" -ExpectedStatus 400 -Body @{
    username = "weak_user_$(Get-Random)"
    email = "weak$(Get-Random)@test.com"
    password = "weak"
    firstName = "Weak"
    lastName = "User"
    role = "Student"
}
Write-TestResult -Success ($result.StatusCode -eq 400)

# ============================================================================
# LOGIN TESTS
# ============================================================================

Write-TestHeader "LOGIN TESTS"

# Test 6: Login with Username
Write-TestName "Login with Username"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/login" -Body @{
    usernameOrEmail = $studentUsername
    password = "SecurePass123!"
}
Write-TestResult -Success $result.Success

# Test 7: Login with Email
Write-TestName "Login with Email"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/login" -Body @{
    usernameOrEmail = $studentEmail
    password = "SecurePass123!"
}
Write-TestResult -Success $result.Success

# Test 8: Invalid Password (Should Fail)
Write-TestName "Reject Invalid Password"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/login" -ExpectedStatus 401 -Body @{
    usernameOrEmail = $studentUsername
    password = "WrongPassword123!"
}
Write-TestResult -Success ($result.StatusCode -eq 401)

# ============================================================================
# PROTECTED ENDPOINTS
# ============================================================================

Write-TestHeader "PROTECTED ENDPOINT TESTS"

# Test 9: Get Current User
Write-TestName "Get Current User Info"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/me" -Token $tokens.Student
Write-TestResult -Success ($result.Success -and $result.Data.username -eq $studentUsername)

# Test 10: No Token (Should Fail)
Write-TestName "Reject Request Without Token"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/me" -ExpectedStatus 401
Write-TestResult -Success ($result.StatusCode -eq 401)

# Test 11: Invalid Token (Should Fail)
Write-TestName "Reject Invalid Token"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/me" -Token "invalid_token" -ExpectedStatus 401
Write-TestResult -Success ($result.StatusCode -eq 401)

# ============================================================================
# ROLE-BASED AUTHORIZATION
# ============================================================================

Write-TestHeader "ROLE-BASED AUTHORIZATION TESTS"

# Test 12: Student Access to Student Endpoint
Write-TestName "Student Access Student Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/student-only" -Token $tokens.Student
Write-TestResult -Success $result.Success

# Test 13: Business Denied Student Endpoint
Write-TestName "Business Denied Student Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/student-only" -Token $tokens.Business -ExpectedStatus 403
Write-TestResult -Success ($result.StatusCode -eq 403)

# Test 14: Business Access to Business Endpoint
Write-TestName "Business Access Business Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/business-only" -Token $tokens.Business
Write-TestResult -Success $result.Success

# Test 15: Student Denied Business Endpoint
Write-TestName "Student Denied Business Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/business-only" -Token $tokens.Student -ExpectedStatus 403
Write-TestResult -Success ($result.StatusCode -eq 403)

# Test 16: Rider Access to Rider Endpoint
Write-TestName "Rider Access Rider Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/rider-only" -Token $tokens.Rider
Write-TestResult -Success $result.Success

# Test 17: Student Access Multi-Role Endpoint
Write-TestName "Student Access Student/Business Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/student-or-business" -Token $tokens.Student
Write-TestResult -Success $result.Success

# Test 18: Business Access Multi-Role Endpoint
Write-TestName "Business Access Student/Business Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/student-or-business" -Token $tokens.Business
Write-TestResult -Success $result.Success

# Test 19: Rider Denied Multi-Role Endpoint
Write-TestName "Rider Denied Student/Business Endpoint"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/student-or-business" -Token $tokens.Rider -ExpectedStatus 403
Write-TestResult -Success ($result.StatusCode -eq 403)

# ============================================================================
# TOKEN REFRESH
# ============================================================================

Write-TestHeader "TOKEN REFRESH TESTS"

# Test 20: Refresh Token
Write-TestName "Refresh Access Token"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/refresh-token" -Body @{
    refreshToken = $tokens.StudentRefresh
}

if ($result.Success) {
    $newAccessToken = $result.Data.accessToken
    $newRefreshToken = $result.Data.refreshToken
    $script:tokens.Student = $newAccessToken
    $script:tokens.StudentRefresh = $newRefreshToken
    Write-TestResult -Success $true
} else {
    Write-TestResult -Success $false -Message $result.Error
}

# Test 21: Use New Access Token
Write-TestName "Use Refreshed Access Token"
$result = Invoke-ApiRequest -Method GET -Endpoint "/api/auth/me" -Token $tokens.Student
Write-TestResult -Success $result.Success

# Test 22: Invalid Refresh Token (Should Fail)
Write-TestName "Reject Invalid Refresh Token"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/refresh-token" -ExpectedStatus 401 -Body @{
    refreshToken = "invalid_refresh_token"
}
Write-TestResult -Success ($result.StatusCode -eq 401)

# ============================================================================
# LOGOUT TESTS
# ============================================================================

Write-TestHeader "LOGOUT TESTS"

# Test 23: Revoke Token
Write-TestName "Revoke Refresh Token"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/revoke-token" -Body @{
    refreshToken = $tokens.StudentRefresh
}
Write-TestResult -Success $result.Success

# Test 24: Use Revoked Token (Should Fail)
Write-TestName "Reject Revoked Refresh Token"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/refresh-token" -ExpectedStatus 401 -Body @{
    refreshToken = $tokens.StudentRefresh
}
Write-TestResult -Success ($result.StatusCode -eq 401)

# Test 25: Revoke All Tokens
Write-TestName "Revoke All User Tokens"
$result = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/revoke-all-tokens" -Token $tokens.Student
Write-TestResult -Success $result.Success

# ============================================================================
# TEST SUMMARY
# ============================================================================

Write-Host "`n"
Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                       TEST SUMMARY                            ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n  Total Tests:  " -NoNewline
Write-Host $total -ForegroundColor Cyan

Write-Host "  ✓ Passed:     " -NoNewline
Write-Host $passed -ForegroundColor Green

Write-Host "  ✗ Failed:     " -NoNewline
Write-Host $failed -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })

$percentage = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 2) } else { 0 }
Write-Host "  Success Rate: " -NoNewline
Write-Host "$percentage%" -ForegroundColor $(if ($percentage -eq 100) { "Green" } elseif ($percentage -ge 80) { "Yellow" } else { "Red" })

Write-Host "`n"

if ($failed -eq 0) {
    Write-Host "🎉 All tests passed! Your authentication system is working perfectly!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some tests failed. Please review the errors above." -ForegroundColor Yellow
    exit 1
}
