// FNBReservation.Modules.Outlet.Infrastructure/Repositories/TableRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;

namespace FNBReservation.Modules.Outlet.Infrastructure.Repositories
{
    public class TableRepository : BaseRepository<TableEntity, OutletDbContext>, ITableRepository
    {
        private readonly ILogger<TableRepository> _logger;

        public TableRepository(
            DbContextFactory<OutletDbContext> contextFactory,
            ILogger<TableRepository> logger)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TableEntity> CreateAsync(TableEntity table)
        {
            _logger.LogInformation("Creating new table {TableNumber} for outlet {OutletId}",
                table.TableNumber, table.OutletId);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                var result = await dbSet.AddAsync(table);
                return result.Entity;
            });
        }

        public async Task<TableEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting table by ID {Id}", id);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet.FirstOrDefaultAsync(t => t.Id == id);
            });
        }

        public async Task<IEnumerable<TableEntity>> GetByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting all tables for outlet {OutletId}", outletId);

            return await ExecuteReadQueryAsync(async dbSet =>
            {
                return await dbSet
                    .Where(t => t.OutletId == outletId)
                    .OrderBy(t => t.Section)
                    .ThenBy(t => t.TableNumber)
                    .ToListAsync();
            });
        }

        public async Task<IEnumerable<string>> GetSectionsByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting distinct sections for outlet {OutletId}", outletId);

            using var context = _contextFactory.CreateReadContext();
            return await context.Tables
                .Where(t => t.OutletId == outletId && t.IsActive)
                .Select(t => t.Section)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        public async Task<int> GetTableCountBySectionAsync(Guid outletId, string section)
        {
            _logger.LogInformation("Getting table count for outlet {OutletId} section {Section}",
                outletId, section);

            using var context = _contextFactory.CreateReadContext();
            return await context.Tables
                .CountAsync(t => t.OutletId == outletId && t.Section == section && t.IsActive);
        }

        public async Task<int> GetTotalCapacityBySectionAsync(Guid outletId, string section)
        {
            _logger.LogInformation("Getting total capacity for outlet {OutletId} section {Section}",
                outletId, section);

            using var context = _contextFactory.CreateReadContext();
            return await context.Tables
                .Where(t => t.OutletId == outletId && t.Section == section && t.IsActive)
                .SumAsync(t => t.Capacity);
        }

        public async Task<TableEntity> UpdateAsync(TableEntity table)
        {
            _logger.LogInformation("Updating table {Id}", table.Id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                dbSet.Update(table);
                return table;
            });
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting table {Id}", id);

            return await ExecuteWriteQueryAsync(async dbSet =>
            {
                var table = await dbSet.FindAsync(id);
                if (table == null)
                {
                    _logger.LogWarning("Table {Id} not found for deletion", id);
                    return false;
                }

                dbSet.Remove(table);
                return true;
            });
        }

        public async Task<int> GetTotalTablesCapacityAsync(Guid outletId)
        {
            _logger.LogInformation("Getting total tables capacity for outlet {OutletId}", outletId);

            using var context = _contextFactory.CreateReadContext();
            return await context.Tables
                .Where(t => t.OutletId == outletId && t.IsActive)
                .SumAsync(t => t.Capacity);
        }

        public async Task<int> GetReservationCapacityAsync(Guid outletId)
        {
            _logger.LogInformation("Getting reservation capacity for outlet {OutletId}", outletId);

            using var context = _contextFactory.CreateReadContext();

            // Get the outlet's reservation allocation percentage
            var outlet = await context.Outlets.FindAsync(outletId);
            if (outlet == null)
            {
                _logger.LogWarning("Outlet {OutletId} not found", outletId);
                return 0;
            }

            // Calculate reservation capacity based on total capacity and allocation percentage
            var totalCapacity = await GetTotalTablesCapacityAsync(outletId);
            var reservationAllocation = outlet.ReservationAllocationPercent;

            return (int)Math.Ceiling(totalCapacity * (reservationAllocation / 100.0));
        }
    }
}