using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Authentication.Core.Entities;

namespace FNBReservation.Modules.Authentication.Core.Interfaces
{
    public interface IAuthRepository
    {
        Task<User> GetUserByCredentialsAsync(string identifier);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByResetTokenAsync(string token);
        Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task UpdateUserAsync(User user);
        Task SaveChangesAsync();
        Task RevokeRefreshTokensAsync(List<RefreshToken> refreshTokens);
    }
}