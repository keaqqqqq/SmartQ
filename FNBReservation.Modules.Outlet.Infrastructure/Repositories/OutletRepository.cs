// FNBReservation.Modules.Outlet.Infrastructure/Repositories/OutletRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using FNBReservation.SharedKernel.Data;
using Microsoft.EntityFrameworkCore.Internal;

namespace FNBReservation.Modules.Outlet.Infrastructure.Repositories
{
    public class OutletRepository : BaseRepository<OutletEntity, OutletDbContext>, IOutletRepository
    {
        private readonly ILogger<OutletRepository> _logger;

        public OutletRepository(
            FNBReservation.SharedKernel.Data.DbContextFactory<OutletDbContext> contextFactory,
            ILogger<OutletRepository> logger)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OutletEntity> CreateAsync(OutletEntity outlet)
        {
            _logger.LogInformation("Creating new outlet: {Name}", outlet.Name);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                var result = await dbSet.AddAsync(outlet);
                return result.Entity;
            });
        }

        public async Task<OutletEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting outlet by ID: {Id}", id);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet.FirstOrDefaultAsync(o => o.Id == id);
            });
        }

        public async Task<OutletEntity> GetByBusinessIdAsync(string outletId)
        {
            _logger.LogInformation("Getting outlet by business ID: {OutletId}", outletId);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet.FirstOrDefaultAsync(o => o.OutletId == outletId);
            });
        }

        public async Task<IEnumerable<OutletEntity>> GetAllAsync()
        {
            _logger.LogInformation("Getting all outlets");

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet.OrderBy(o => o.Name).ToListAsync();
            });
        }

        public async Task<OutletEntity> UpdateAsync(OutletEntity outlet)
        {
            _logger.LogInformation("Updating outlet: {Id}", outlet.Id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                dbSet.Update(outlet);
                return outlet;
            });
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting outlet: {Id}", id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                var outlet = await dbSet.FindAsync(id);

                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found for deletion: {Id}", id);
                    return false;
                }

                dbSet.Remove(outlet);
                return true;
            });
        }

        public async Task<IEnumerable<OutletChange>> GetOutletChangesAsync(Guid outletId)
        {
            _logger.LogInformation("Getting changes for outlet: {OutletId}", outletId);

            using var context = _contextFactory.CreateReadContext();
            return await context.OutletChanges
                .Where(c => c.OutletId == outletId)
                .OrderByDescending(c => c.RequestedAt)
                .ToListAsync();
        }

        public async Task<OutletChange> GetOutletChangeByIdAsync(Guid changeId)
        {
            _logger.LogInformation("Getting outlet change by ID: {ChangeId}", changeId);

            using var context = _contextFactory.CreateReadContext();
            return await context.OutletChanges
                .Include(c => c.Outlet)
                .FirstOrDefaultAsync(c => c.Id == changeId);
        }

        public async Task<OutletChange> CreateOutletChangeAsync(OutletChange change)
        {
            _logger.LogInformation("Creating outlet change for outlet: {OutletId}, field: {FieldName}",
            change.OutletId, change.FieldName);

            using var context = _contextFactory.CreateWriteContext();
            await context.OutletChanges.AddAsync(change);
            await context.SaveChangesAsync();
            return change;
        }

        public async Task<OutletChange> UpdateOutletChangeAsync(OutletChange change)
        {
            _logger.LogInformation("Updating outlet change: {ChangeId}", change.Id);

            using var context = _contextFactory.CreateWriteContext();
            context.OutletChanges.Update(change);
            await context.SaveChangesAsync();
            return change;
        }
    }
}