// FNBReservation.Modules.Outlet.Infrastructure/Services/TableTypeService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.Infrastructure.Services
{
    public class TableTypeService : ITableTypeService
    {
        private readonly ITableService _tableService;
        private readonly IPeakHourService _peakHourService;
        private readonly ILogger<TableTypeService> _logger;

        public TableTypeService(
            ITableService tableService,
            IPeakHourService peakHourService,
            ILogger<TableTypeService> logger)
        {
            _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
            _peakHourService = peakHourService ?? throw new ArgumentNullException(nameof(peakHourService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<TableDto>> GetReservationTablesAsync(Guid outletId, DateTime dateTime)
        {
            _logger.LogInformation("Getting reservation tables for outlet {OutletId} at {DateTime}", outletId, dateTime);

            // Get all active tables for the outlet
            var allTables = await _tableService.GetTablesByOutletIdAsync(outletId);
            var activeTables = allTables.Where(t => t.IsActive).ToList();

            if (!activeTables.Any())
            {
                _logger.LogWarning("No active tables found for outlet {OutletId}", outletId);
                return new List<TableDto>();
            }

            // Get reservation allocation percentage for this time
            int reservationAllocationPercent = await _peakHourService.GetCurrentReservationAllocationAsync(outletId, dateTime);

            // If allocation is 0%, there are no reservation tables
            if (reservationAllocationPercent <= 0)
            {
                _logger.LogInformation("Reservation allocation is 0% for outlet {OutletId} at {DateTime}, returning no tables",
                    outletId, dateTime);
                return new List<TableDto>();
            }

            // Calculate number of tables for reservations
            int totalTables = activeTables.Count;
            int reservationTablesCount = (int)Math.Ceiling(totalTables * (reservationAllocationPercent / 100.0));

            // Select tables for reservations (prefer larger capacity tables for reservations)
            var reservationTables = activeTables
                .OrderByDescending(t => t.Capacity)
                .Take(reservationTablesCount)
                .ToList();

            _logger.LogInformation("Selected {Count} tables for reservations out of {Total} total tables for outlet {OutletId}",
                reservationTables.Count, totalTables, outletId);

            return reservationTables;
        }

        public async Task<List<TableDto>> GetQueueTablesAsync(Guid outletId, DateTime dateTime)
        {
            _logger.LogInformation("Getting queue tables for outlet {OutletId} at {DateTime}", outletId, dateTime);

            // Get all active tables for the outlet
            var allTables = await _tableService.GetTablesByOutletIdAsync(outletId);
            var activeTables = allTables.Where(t => t.IsActive).ToList();

            if (!activeTables.Any())
            {
                _logger.LogWarning("No active tables found for outlet {OutletId}", outletId);
                return new List<TableDto>();
            }

            // Get reservation allocation percentage for this time
            int reservationAllocationPercent = await _peakHourService.GetCurrentReservationAllocationAsync(outletId, dateTime);

            // If allocation is 100%, there are no queue tables
            if (reservationAllocationPercent >= 100)
            {
                _logger.LogInformation("Reservation allocation is 100% for outlet {OutletId} at {DateTime}, returning no queue tables",
                    outletId, dateTime);
                return new List<TableDto>();
            }

            // Calculate number of tables for reservations
            int totalTables = activeTables.Count;
            int reservationTablesCount = (int)Math.Ceiling(totalTables * (reservationAllocationPercent / 100.0));

            // Ensure we leave at least one table for queue if there are tables available
            // and the allocation isn't 100%
            int queueTablesCount = Math.Max(1, totalTables - reservationTablesCount);

            // Select tables for queue (smaller tables first for queue)
            var queueTables = activeTables
                .OrderBy(t => t.Capacity)
                .Take(queueTablesCount)
                .ToList();

            _logger.LogInformation("Selected {Count} tables for queue out of {Total} total tables for outlet {OutletId}",
                queueTables.Count, totalTables, outletId);

            return queueTables;
        }

        public async Task<bool> IsReservationTableAsync(Guid outletId, Guid tableId, DateTime dateTime)
        {
            var reservationTables = await GetReservationTablesAsync(outletId, dateTime);
            return reservationTables.Any(t => t.Id == tableId);
        }

        public async Task<bool> IsQueueTableAsync(Guid outletId, Guid tableId, DateTime dateTime)
        {
            var queueTables = await GetQueueTablesAsync(outletId, dateTime);
            return queueTables.Any(t => t.Id == tableId);
        }
    }
}