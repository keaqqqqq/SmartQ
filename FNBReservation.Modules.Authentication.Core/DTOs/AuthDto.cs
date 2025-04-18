using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Authentication.Core.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Username or email is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public bool RememberMe { get; set; } = false;
    }

    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string NewPassword { get; set; }
    }

    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ExpiresIn { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }

        public Guid? OutletId { get; set; }
    }

    public class TokenResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
        public Guid? OutletId { get; set; }
    }

    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TestEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
