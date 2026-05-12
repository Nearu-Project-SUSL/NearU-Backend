using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NearU_Backend_Revised.Models;

public partial class User
{
    [Key]
    public string Id { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string CreatedDate { get; set; } = null!;

    public string? LastLoginDate { get; set; }

    public int IsActive { get; set; }

    public string? MobileNumber { get; set; }

    public string? StudentId { get; set; }

    public string? Faculty { get; set; }

    public string? Year { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? DateOfBirth { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
