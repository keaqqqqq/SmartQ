// FNBReservation.Modules.Outlet.Infrastructure/Repositories/OutletRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Modules.Outlet.Infrastructure.Repositories
{
    public class OutletRepository : IOutletRepository
    {
        private readonly OutletDbContext _dbContext;
        private readonly ILogger<OutletRepository> _logger;

        public OutletRepository(OutletDbContext dbContext, ILogger<OutletRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OutletEntity> CreateAsync(OutletEntity outlet)
        {
            _logger.LogInformation("Creating new outlet: {Name}", outlet.Name);

            await _dbContext.Outlets.AddAsync(outlet);
            await _dbContext.SaveChangesAsync();

            return outlet;
        }

        public async Task<OutletEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting outlet by ID: {Id}", id);

            return await _dbContext.Outlets
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<OutletEntity> GetByBusinessIdAsync(string outletId)
        {
            _logger.LogInformation("Getting outlet by business ID: {OutletId}", outletId);

            return await _dbContext.Outlets
                .FirstOrDefaultAsync(o => o.OutletId == outletId);
        }

        public async Task<IEnumerable<OutletEntity>> GetAllAsync()
        {
            _logger.LogInformation("Getting all outlets");

            return await _dbContext.Outlets
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        public async Task<OutletEntity> UpdateAsync(OutletEntity outlet)
        {
            _logger.LogInformation("Updating outlet: {Id}", outlet.Id);

            _dbContext.Outlets.Update(outlet);
            await _dbContext.SaveChangesAsync();

            return outlet;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting outlet: {Id}", id);

            var outlet = await _dbContext.Outlets.FindAsync(id);

            if (outlet == null)
            {
                _logger.LogWarning("Outlet not found for deletion: {Id}", id);
                return false;
            }

            _dbContext.Outlets.Remove(outlet);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<OutletChange>> GetOutletChangesAsync(Guid outletId)
        {
            _logger.LogInformation("Getting changes for outlet: {OutletId}", outletId);

            return await _dbContext.OutletChanges
                .Where(c => c.OutletId == outletId)
                .OrderByDescending(c => c.RequestedAt)
                .ToListAsync();
        }

        public async Task<OutletChange> GetOutletChangeByIdAsync(Guid changeId)
        {
            _logger.LogInformation("Getting outlet change by ID: {ChangeId}", changeId);

            return await _dbContext.OutletChanges
                .Include(c => c.Outlet)
                .FirstOrDefaultAsync(c => c.Id == changeId);
        }

        public async Task<OutletChange> CreateOutletChangeAsync(OutletChange change)
        {
            _logger.LogInformation("Creating outlet change for outlet: {OutletId}, field: {FieldName}", change.OutletId, change.FieldName);

            await _dbContext.OutletChanges.AddAsync(change);
            await _dbContext.SaveChangesAsync();

            return change;
        }

        public async Task<OutletChange> UpdateOutletChangeAsync(OutletChange change)
        {
            _logger.LogInformation("Updating outlet change: {ChangeId}", change.Id);

            _dbContext.OutletChanges.Update(change);
            await _dbContext.SaveChangesAsync();

            return change;
        }
    }
}