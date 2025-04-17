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
using Microsoft.AspNetCore.Http;

namespace FNBReservation.Modules.Authentication.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly FNBDbContext _dbContext;
        private readonly ILogger<TokenService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(
            IConfiguration configuration,
            FNBDbContext dbContext,
            ILogger<TokenService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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

                // Set the JWT token as a HTTP-only cookie
                SetAccessTokenCookie(encodedToken);

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

            // Set refresh token as a HTTP-only cookie
            SetRefreshTokenCookie(refreshToken, expiryTime);

            return (refreshToken, expiryTime);
        }

        public async Task<TokenResult> RefreshTokenAsync(string refreshToken = null)
        {
            // If refresh token wasn't provided in the request body, try to get it from the cookie
            if (string.IsNullOrEmpty(refreshToken) && _httpContextAccessor.HttpContext != null)
            {
                refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new TokenResult { Success = false, ErrorMessage = "Refresh token not found" };
                }
            }

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

                // Clear the cookies if token is expired
                ClearAuthCookies();

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
                ExpiresIn = 900, // 15 minutes in seconds
                Role = user.Role,
                Username = user.Username,
                OutletId = user.Role == "OutletStaff" ? user.OutletId : null
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            // If refresh token wasn't provided, try to get it from the cookie
            if (string.IsNullOrEmpty(refreshToken) && _httpContextAccessor.HttpContext != null)
            {
                refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }
            }

            var storedToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync();

            // Clear the auth cookies
            ClearAuthCookies();

            return true;
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                // If token wasn't provided in the request body, try to get it from the cookie
                if (string.IsNullOrEmpty(token) && _httpContextAccessor.HttpContext != null)
                {
                    token = _httpContextAccessor.HttpContext.Request.Cookies["accessToken"];

                    if (string.IsNullOrEmpty(token))
                    {
                        throw new SecurityTokenException("Access token not found");
                    }
                }

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

        // Helper methods for managing cookies
        private void SetAccessTokenCookie(string token)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _configuration.GetValue<bool>("Cookies:SecureOnly", true), // Set to true in production
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(15), // Match token expiry
                    Path = "/"
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append("accessToken", token, cookieOptions);
                _logger.LogDebug("Access token cookie set");
            }
        }

        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _configuration.GetValue<bool>("Cookies:SecureOnly", true), // Set to true in production
                    SameSite = SameSiteMode.Strict,
                    Expires = expires,
                    Path = "/"
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", token, cookieOptions);
                _logger.LogDebug("Refresh token cookie set");
            }
        }

        private void ClearAuthCookies()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("accessToken");
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("refreshToken");
                _logger.LogDebug("Auth cookies cleared");
            }
        }
    }
}