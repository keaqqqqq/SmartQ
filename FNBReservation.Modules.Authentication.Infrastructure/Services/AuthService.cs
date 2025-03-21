using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Authentication.Core.DTOs;
using FNBReservation.Modules.Authentication.Core.Entities;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Modules.Authentication.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Modules.Authentication.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly FNBDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(FNBDbContext dbContext, ITokenService tokenService, IEmailService emailService, ILogger<AuthService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public async Task<AuthResult> AuthenticateAsync(LoginDto loginDto)
        {
            // Find the user by username, email, or userId
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(s =>
                    s.Username == loginDto.Username ||
                    s.Email == loginDto.Username ||
                    s.UserId == loginDto.Username);

            if (user == null || !user.IsActive)
                return new AuthResult { Success = false, ErrorMessage = "Invalid credentials" };

            // Verify password
            if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash))
                return new AuthResult { Success = false, ErrorMessage = "Invalid credentials" };

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user);
            var (refreshToken, expiryTime) = _tokenService.GenerateRefreshToken();

            // Store refresh token in the database
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                ExpiryTime = expiryTime,
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                UserId = user.Id
            };

            await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            // Calculate expiry time in seconds
            var expiresIn = 900; // 15 minutes in seconds for access token

            return new AuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn,
                Role = user.Role,
                Username = user.Username
            };
        }

        public async Task<PasswordResetResult> ForgotPasswordAsync(string email)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(s => s.Email == email && s.IsActive);

            if (user == null)
                return new PasswordResetResult { Success = true }; // Don't reveal if email exists

            // Generate a reset token
            var resetToken = GenerateRandomToken();
            var tokenExpiry = DateTime.UtcNow.AddHours(24); // Token valid for 24 hours

            // Save token to user record
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = tokenExpiry;

            await _dbContext.SaveChangesAsync();

            // Send email with reset token
            await _emailService.SendPasswordResetEmailAsync(email, resetToken);

            return new PasswordResetResult { Success = true };
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(s =>
                s.PasswordResetToken == token &&
                s.PasswordResetTokenExpiry > DateTime.UtcNow &&
                s.IsActive);

            if (user == null)
                return new PasswordResetResult { Success = false, ErrorMessage = "Invalid or expired token" };

            // Create new password hash
            user.PasswordHash = CreatePasswordHash(newPassword);

            // Update user record
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new PasswordResetResult { Success = true };
        }

        public async Task LogoutAsync(string userId)
        {
            _logger.LogInformation($"LogoutAsync called with userId: {userId}");

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                _logger.LogError($"Failed to parse userId as Guid: {userId}");
                throw new ArgumentException($"Invalid userId format. Cannot parse '{userId}' as a Guid.");
            }

            _logger.LogInformation($"Successfully parsed userId to Guid: {userGuid}");

            try
            {
                // Find all active refresh tokens for this user and revoke them
                var refreshTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == userGuid && !rt.IsRevoked)
                    .ToListAsync();

                _logger.LogInformation($"Found {refreshTokens.Count} active refresh tokens for user");

                if (refreshTokens.Count == 0)
                {
                    _logger.LogInformation("No active refresh tokens found for user");
                    return;
                }

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                    _logger.LogDebug($"Marked token {token.Id} as revoked");
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully revoked tokens and saved changes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking refresh tokens for user {userGuid}");
                throw; // Rethrow to let the controller handle it
            }
        }

        #region Helper Methods
        // Add this package: Microsoft.AspNetCore.Identity
        private bool VerifyPasswordHash(string password, string storedHash)
        {
            try
            {
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
                var result = passwordHasher.VerifyHashedPassword(null, storedHash, password);
                Console.WriteLine($"Identity verification result: {result}");
                return result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success
                    || result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying password: {ex.Message}");
                return false;
            }
        }

        private string CreatePasswordHash(string password)
        {
            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            return passwordHasher.HashPassword(null, password);
        }


        private string GenerateRandomToken()
        {
            var randomBytes = new byte[40];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
        #endregion
    }
}