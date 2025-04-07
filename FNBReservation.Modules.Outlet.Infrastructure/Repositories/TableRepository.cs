// FNBReservation.Modules.Outlet.Infrastructure/Repositories/TableRepository.cs (Updated)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;

namespace FNBReservation.Modules.Outlet.Infrastructure.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly OutletDbContext _dbContext;
        private readonly ILogger<TableRepository> _logger;

        public TableRepository(OutletDbContext dbContext, ILogger<TableRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TableEntity> CreateAsync(TableEntity table)
        {
            _logger.LogInformation("Creating new table {TableNumber} for outlet {OutletId}",
                table.TableNumber, table.OutletId);

            await _dbContext.Tables.AddAsync(table);
            await _dbContext.SaveChangesAsync();

            return table;
        }

        public async Task<TableEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting table by ID {Id}", id);

            return await _dbContext.Tables
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TableEntity>> GetByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting all tables for outlet {OutletId}", outletId);

            return await _dbContext.Tables
                .Where(t => t.OutletId == outletId)
                .OrderBy(t => t.Section)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetSectionsByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting distinct sections for outlet {OutletId}", outletId);

            return await _dbContext.Tables
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

            return await _dbContext.Tables
                .CountAsync(t => t.OutletId == outletId && t.Section == section && t.IsActive);
        }

        public async Task<int> GetTotalCapacityBySectionAsync(Guid outletId, string section)
        {
            _logger.LogInformation("Getting total capacity for outlet {OutletId} section {Section}",
                outletId, section);

            return await _dbContext.Tables
                .Where(t => t.OutletId == outletId && t.Section == section && t.IsActive)
                .SumAsync(t => t.Capacity);
        }

        public async Task<TableEntity> UpdateAsync(TableEntity table)
        {
            _logger.LogInformation("Updating table {Id}", table.Id);

            _dbContext.Tables.Update(table);
            await _dbContext.SaveChangesAsync();

            return table;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting table {Id}", id);

            var table = await _dbContext.Tables.FindAsync(id);
            if (table == null)
            {
                _logger.LogWarning("Table {Id} not found for deletion", id);
                return false;
            }

            _dbContext.Tables.Remove(table);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalTablesCapacityAsync(Guid outletId)
        {
            _logger.LogInformation("Getting total tables capacity for outlet {OutletId}", outletId);

            return await _dbContext.Tables
                .Where(t => t.OutletId == outletId && t.IsActive)
                .SumAsync(t => t.Capacity);
        }

        public async Task<int> GetReservationCapacityAsync(Guid outletId)
        {
            _logger.LogInformation("Getting reservation capacity for outlet {OutletId}", outletId);

            // Get the outlet's reservation allocation percentage
            var outlet = await _dbContext.Outlets.FindAsync(outletId);
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