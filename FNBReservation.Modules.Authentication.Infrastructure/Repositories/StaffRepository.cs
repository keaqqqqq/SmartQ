using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Authentication.Core.Entities;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Modules.Authentication.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;

namespace FNBReservation.Modules.Authentication.Infrastructure.Repositories
{
    public class StaffRepository : BaseRepository<User, FNBDbContext>, IStaffRepository
    {
        private readonly ILogger<StaffRepository> _logger;

        public StaffRepository(
            DbContextFactory<FNBDbContext> contextFactory,
            ILogger<StaffRepository> logger)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> GetStaffByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting staff by ID: {Id}", id);

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Staff");
        }

        public async Task<IEnumerable<User>> GetStaffByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting staff by outlet ID: {OutletId}", outletId);

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .Where(u => u.OutletId == outletId && u.UserType == "Staff")
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllStaffAsync()
        {
            _logger.LogInformation("Getting all staff");

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .Where(u => u.UserType == "Staff")
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            _logger.LogInformation("Getting user by username: {Username}", username);

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            _logger.LogInformation("Getting user by email: {Email}", email);

            using var context = _contextFactory.CreateReadContext();
            return await context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateStaffAsync(User user)
        {
            _logger.LogInformation("Creating new staff: {Username}", user.Username);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff: {Username}", user.Username);
                throw;
            }
        }

        public async Task<User> UpdateStaffAsync(User user)
        {
            _logger.LogInformation("Updating staff: {Id}", user.Id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Find existing user to ensure proper tracking
                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == user.Id && u.UserType == "Staff");

                if (existingUser == null)
                {
                    _logger.LogWarning("Staff not found for update: {Id}", user.Id);
                    throw new KeyNotFoundException($"Staff with ID {user.Id} not found");
                }

                // Update properties
                context.Entry(existingUser).CurrentValues.SetValues(user);
                await context.SaveChangesAsync();
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff: {Id}", user.Id);
                throw;
            }
        }

        public async Task<bool> DeleteStaffAsync(Guid id)
        {
            _logger.LogInformation("Deleting staff: {Id}", id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                var staff = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Staff");

                if (staff == null)
                {
                    _logger.LogWarning("Staff not found for deletion: {Id}", id);
                    return false;
                }

                context.Users.Remove(staff);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff: {Id}", id);
                throw;
            }
        }
    }
}