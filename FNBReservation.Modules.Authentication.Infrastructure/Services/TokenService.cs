using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using FNBReservation.Modules.Authentication.Core.DTOs;
using FNBReservation.Modules.Authentication.Core.Entities;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Modules.Authentication.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Modules.Authentication.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly FNBDbContext _dbContext;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, FNBDbContext dbContext, ILogger<TokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateAccessToken(User user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

                _logger.LogInformation("Generating access token for user: {Username}, ID: {Id}, UserId: {UserId}, Role: {Role}, OutletId: {OutletId}",
                    user.Username, user.Id, user.UserId, user.Role, user.OutletId);

                var claimsList = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.UserId)
                };

                // Add OutletId claim for staff users (not for admins)
                if (user.OutletId.HasValue && user.Role == "OutletStaff")
                {
                    claimsList.Add(new Claim("OutletId", user.OutletId.Value.ToString()));
                    _logger.LogDebug("Added OutletId claim with value: {OutletId}", user.OutletId.Value);
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claimsList),
                    Expires = DateTime.UtcNow.AddMinutes(15), // 15 minutes
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                    ),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"]
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var encodedToken = tokenHandler.WriteToken(token);

                // Log the token format for debugging (don't do this in production)
                _logger.LogDebug("Token starts with: {TokenStart}...",
                    encodedToken.Substring(0, Math.Min(10, encodedToken.Length)));
                _logger.LogDebug("Token contains expected dots: {DotsCount}",
                    encodedToken.Count(c => c == '.'));

                return encodedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token");
                throw;
            }
        }

        public (string refreshToken, DateTime expiryTime) GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var refreshToken = Convert.ToBase64String(randomNumber);
            var expiryTime = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days

            return (refreshToken, expiryTime);
        }

        public async Task<TokenResult> RefreshTokenAsync(string refreshToken)
        {
            // Find the refresh token in the database
            var storedToken = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null)
            {
                return new TokenResult { Success = false, ErrorMessage = "Invalid refresh token" };
            }

            // Check if token is expired
            if (storedToken.ExpiryTime < DateTime.UtcNow)
            {
                storedToken.IsRevoked = true;
                await _dbContext.SaveChangesAsync();
                return new TokenResult { Success = false, ErrorMessage = "Refresh token expired" };
            }

            var user = storedToken.User;

            // Revoke the used refresh token
            storedToken.IsRevoked = true;

            // Generate new tokens
            var accessToken = GenerateAccessToken(user);
            var (newRefreshToken, expiryTime) = GenerateRefreshToken();

            // Store the new refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                ExpiryTime = expiryTime,
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                UserId = user.Id
            };

            await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            return new TokenResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 86400, // 15 minutes in seconds
                Role = user.Role,
                Username = user.Username
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false, // Allow expired tokens
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]))
                };

                var tokenHandler = new JwtSecurityTokenHandler();

                // Log token format before validation
                _logger.LogDebug("Validating token: {TokenStart}...",
                    token.Length > 10 ? token.Substring(0, 10) + "..." : token);

                if (string.IsNullOrEmpty(token) || !token.Contains('.'))
                {
                    _logger.LogWarning("Token is malformed - missing dots or empty");
                    throw new SecurityTokenException("Invalid token format");
                }

                SecurityToken securityToken;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                throw;
            }
        }
    }
}