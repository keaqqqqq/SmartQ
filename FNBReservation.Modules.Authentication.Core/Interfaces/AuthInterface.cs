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
        string GenerateAccessToken(User user);
        (string refreshToken, DateTime expiryTime) GenerateRefreshToken();
        Task<TokenResult> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetToken);
    }
}
