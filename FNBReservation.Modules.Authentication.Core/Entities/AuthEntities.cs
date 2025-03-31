using System;
using System.Collections.Generic;

namespace FNBReservation.Modules.Authentication.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string? FullName { get; set; } // Make it nullable with '?'
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // Admin, OutletStaff
        public string UserType { get; set; } // Admin, Staff
        public Guid? OutletId { get; set; } // Null for Admin, populated for Staff
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }

    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
