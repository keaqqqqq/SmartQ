using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Authentication.Core.Entities;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Modules.Authentication.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;
using Microsoft.EntityFrameworkCore.Internal;

namespace FNBReservation.Modules.Authentication.Infrastructure.Repositories
{
    public class AuthRepository : BaseRepository<User, FNBDbContext>, IAuthRepository
    {
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            FNBReservation.SharedKernel.Data.DbContextFactory<FNBDbContext> contextFactory,
            ILogger<AuthRepository> logger)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> GetUserByCredentialsAsync(string identifier)
        {
            _logger.LogInformation("Getting user by credentials: {Identifier}", identifier);

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .FirstOrDefaultAsync(s =>
                    s.Username == identifier ||
                    s.Email == identifier ||
                    s.UserId == identifier);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            _logger.LogInformation("Getting user by email: {Email}", email);

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .FirstOrDefaultAsync(s => s.Email == email && s.IsActive);
        }

        public async Task<User> GetUserByResetTokenAsync(string token)
        {
            _logger.LogInformation("Getting user by reset token");

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .FirstOrDefaultAsync(s =>
                    s.PasswordResetToken == token &&
                    s.PasswordResetTokenExpiry > DateTime.UtcNow &&
                    s.IsActive);
        }

        public async Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Getting active refresh tokens for user: {UserId}", userId);

            using var context = _contextFactory.CreateReadContext();
            return await context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            _logger.LogInformation("Adding refresh token for user: {UserId}", refreshToken.UserId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.RefreshTokens.AddAsync(refreshToken);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding refresh token for user: {UserId}", refreshToken.UserId);
                throw;
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            _logger.LogInformation("Updating user: {UserId}", user.Id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Find existing user to ensure proper tracking
                var existingUser = await context.Users.FindAsync(user.Id);
                if (existingUser == null)
                {
                    _logger.LogWarning("User not found for update: {UserId}", user.Id);
                    throw new KeyNotFoundException($"User with ID {user.Id} not found");
                }

                // Update properties
                context.Entry(existingUser).CurrentValues.SetValues(user);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
                throw;
            }
        }

        public async Task SaveChangesAsync()
        {
            _logger.LogInformation("Saving changes to database");

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public async Task RevokeRefreshTokensAsync(List<RefreshToken> refreshTokens)
        {
            if (refreshTokens == null || !refreshTokens.Any())
            {
                _logger.LogInformation("No refresh tokens to revoke");
                return;
            }

            _logger.LogInformation("Revoking {Count} refresh tokens", refreshTokens.Count);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                foreach (var tokenInfo in refreshTokens)
                {
                    // Load each token from the database to ensure proper tracking
                    var token = await context.RefreshTokens.FindAsync(tokenInfo.Id);
                    if (token != null)
                    {
                        token.IsRevoked = true;
                        _logger.LogDebug("Marked token {TokenId} as revoked", token.Id);
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully revoked tokens and saved changes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh tokens");
                throw;
            }
        }
    }
}