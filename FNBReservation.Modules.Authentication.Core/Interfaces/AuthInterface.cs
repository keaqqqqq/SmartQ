using System.Security.Claims;
using System.Threading.Tasks;
using FNBReservation.Modules.Authentication.Core.DTOs;
using FNBReservation.Modules.Authentication.Core.Entities;

namespace FNBReservation.Modules.Authentication.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(LoginDto loginDto);
        Task<PasswordResetResult> ForgotPasswordAsync(string email);
        Task<PasswordResetResult> ResetPasswordAsync(string token, string newPassword);
        Task LogoutAsync(string userId);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(User user); // Now sets HTTP-only cookie
        (string refreshToken, DateTime expiryTime) GenerateRefreshToken(); // Now sets HTTP-only cookie
        Task<TokenResult> RefreshTokenAsync(string refreshToken = null); // Can now work with cookie
        Task<bool> RevokeRefreshTokenAsync(string refreshToken = null); // Can now work with cookie
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token = null); // Can now work with cookie
    }

    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetToken);
    }
}
