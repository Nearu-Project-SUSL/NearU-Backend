using Xunit;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Configuration;

/// <summary>
/// Unit tests for core models and configurations
/// </summary>
public class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_WithValidData_ReturnsCorrectStructure()
    {
        // Arrange
        var message = "Operation successful";
        var data = "test data";

        // Act
        var response = ApiResponse<string>.SuccessResponse(message, data);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Equal(data, response.Data);
    }

    [Fact]
    public void FailResponse_WithMessage_ReturnsFailedState()
    {
        // Arrange
        var message = "Operation failed";

        // Act
        var response = ApiResponse<string>.FailResponse(message);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void SuccessResponse_WithNullData_AllowsNullData()
    {
        // Arrange
        var message = "Success with no data";

        // Act
        var response = ApiResponse<object?>.SuccessResponse(message, null!);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void FailResponse_PreservesErrorMessage()
    {
        // Arrange
        var errorMessage = "User not found";

        // Act
        var response = ApiResponse<int>.FailResponse(errorMessage);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(errorMessage, response.Message);
    }
}

/// <summary>
/// Unit tests for User model validation
/// </summary>
public class UserModelTests
{
    [Fact]
    public void User_NewInstance_HasDefaultCollections()
    {
        // Act
        var user = new User
        {
            Id = "test-id",
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            Role = "User",
            CreatedDate = DateTime.UtcNow.ToString()
        };

        // Assert
        Assert.NotNull(user.RefreshTokens);
        Assert.IsType<List<RefreshToken>>(user.RefreshTokens);
        Assert.Empty(user.RefreshTokens);
    }

    [Fact]
    public void User_WithAllProperties_StoresValuesCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var username = "john_doe";
        var email = "john@example.com";
        var role = "Admin";
        var mobileNumber = "1234567890";
        var faculty = "Engineering";
        var year = "3";

        // Act
        var user = new User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = "hash",
            Role = role,
            CreatedDate = DateTime.UtcNow.ToString(),
            MobileNumber = mobileNumber,
            Faculty = faculty,
            Year = year,
            IsActive = 1
        };

        // Assert
        Assert.Equal(userId, user.Id);
        Assert.Equal(username, user.Username);
        Assert.Equal(email, user.Email);
        Assert.Equal(role, user.Role);
        Assert.Equal(mobileNumber, user.MobileNumber);
        Assert.Equal(faculty, user.Faculty);
        Assert.Equal(year, user.Year);
        Assert.Equal(1, user.IsActive);
    }
}

/// <summary>
/// Unit tests for JWT configuration
/// </summary>
public class JwtSettingsTests
{
    [Fact]
    public void JwtSettings_WithDefaultValues_HasCorrectExpiries()
    {
        // Arrange & Act
        var settings = new JwtSettings();

        // Assert
        Assert.Equal(15, settings.AccessTokenExpiryInMinutes);
        Assert.Equal(7, settings.RefreshTokenExpiryInDays);
    }

    [Fact]
    public void JwtSettings_CanBeConfiguredWithCustomValues()
    {
        // Arrange
        var settings = new JwtSettings
        {
            SecretKey = "my-secret-key-that-is-at-least-32-characters-long!!!",
            Issuer = "https://example.com",
            Audience = "mobile-app",
            AccessTokenExpiryInMinutes = 30,
            RefreshTokenExpiryInDays = 14
        };

        // Act & Assert
        Assert.Equal("my-secret-key-that-is-at-least-32-characters-long!!!", settings.SecretKey);
        Assert.Equal("https://example.com", settings.Issuer);
        Assert.Equal("mobile-app", settings.Audience);
        Assert.Equal(30, settings.AccessTokenExpiryInMinutes);
        Assert.Equal(14, settings.RefreshTokenExpiryInDays);
    }

    [Fact]
    public void JwtSettings_HasDefaultEmptyStrings()
    {
        // Act
        var settings = new JwtSettings();

        // Assert
        Assert.Equal(string.Empty, settings.SecretKey);
        Assert.Equal(string.Empty, settings.Issuer);
        Assert.Equal(string.Empty, settings.Audience);
    }
}

/// <summary>
/// Unit tests for RefreshToken model
/// </summary>
public class RefreshTokenModelTests
{
    [Fact]
    public void RefreshToken_WhenNotRevoked_IsRevokedReturnsFalse()
    {
        // Arrange
        var tokenValue = "random-token-string";
        var userId = "user-123";
        var expiryDate = DateTime.UtcNow.AddDays(7);

        // Act
        var token = new RefreshToken
        {
            Id = 1,
            Token = tokenValue,
            UserId = userId,
            ExpiryDate = expiryDate,
            CreatedDate = DateTime.UtcNow,
            RevokedDate = null
        };

        // Assert
        Assert.Equal(tokenValue, token.Token);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(expiryDate, token.ExpiryDate);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_WhenNotExpired_IsExpiredReturnsFalse()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);

        // Act
        var token = new RefreshToken
        {
            Id = 2,
            Token = "token-value",
            UserId = "user-456",
            ExpiryDate = futureDate,
            CreatedDate = DateTime.UtcNow
        };

        // Assert
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_WhenNotRevokedAndNotExpired_IsActiveReturnsTrue()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);

        // Act
        var token = new RefreshToken
        {
            Id = 3,
            Token = "active-token",
            UserId = "user-789",
            ExpiryDate = futureDate,
            CreatedDate = DateTime.UtcNow,
            RevokedDate = null
        };

        // Assert
        Assert.True(token.IsActive);
    }

    [Fact]
    public void RefreshToken_WhenRevoked_IsRevokedReturnsTrue()
    {
        // Arrange
        var revokedDate = DateTime.UtcNow.AddHours(-1);

        // Act
        var token = new RefreshToken
        {
            Id = 4,
            Token = "revoked-token",
            UserId = "user-000",
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedDate = DateTime.UtcNow,
            RevokedDate = revokedDate,
            ReasonRevoked = "User logged out"
        };

        // Assert
        Assert.True(token.IsRevoked);
        Assert.Equal(revokedDate, token.RevokedDate);
        Assert.Equal("User logged out", token.ReasonRevoked);
    }
}
