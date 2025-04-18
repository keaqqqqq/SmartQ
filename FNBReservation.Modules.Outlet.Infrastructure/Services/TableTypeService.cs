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

        // Define what constitutes a large table (tables with capacity greater than this value)
        private const int LARGE_TABLE_THRESHOLD = 6;

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

            // If allocation is 100%, all tables are for reservations
            if (reservationAllocationPercent >= 100)
            {
                _logger.LogInformation("Reservation allocation is 100% for outlet {OutletId} at {DateTime}, returning all tables",
                    outletId, dateTime);
                return activeTables;
            }

            // Get all large tables first - these ALWAYS go to reservations
            var largeTables = activeTables.Where(t => t.Capacity > LARGE_TABLE_THRESHOLD).ToList();
            _logger.LogInformation("Found {Count} large tables with capacity > {Threshold}",
                largeTables.Count, LARGE_TABLE_THRESHOLD);

            // Calculate the remaining percentage target after allocating large tables
            int totalCapacity = activeTables.Sum(t => t.Capacity);
            int largeTablesCapacity = largeTables.Sum(t => t.Capacity);
            int remainingCapacity = totalCapacity - largeTablesCapacity;

            // Calculate how much more capacity we need for reservations
            int targetReservationCapacity = (int)Math.Ceiling(totalCapacity * (reservationAllocationPercent / 100.0));
            int additionalCapacityNeeded = Math.Max(0, targetReservationCapacity - largeTablesCapacity);

            _logger.LogInformation("Large tables capacity: {LargeCapacity}, Total target reservation capacity: {TargetCapacity}, Additional needed: {AdditionalNeeded}",
                largeTablesCapacity, targetReservationCapacity, additionalCapacityNeeded);

            // Get remaining tables (non-large)
            var remainingTables = activeTables.Where(t => t.Capacity <= LARGE_TABLE_THRESHOLD).ToList();

            // If we've already exceeded or met our target with just large tables, don't allocate more tables
            if (largeTablesCapacity >= targetReservationCapacity)
            {
                _logger.LogInformation("Large tables already exceed target reservation capacity. Keeping all {Count} large tables for reservations.",
                    largeTables.Count);
                return largeTables;
            }

            // Allocate additional tables from remaining tables, prioritizing larger capacities within the remaining group
            var additionalTables = AllocateAdditionalTables(remainingTables, additionalCapacityNeeded);

            // Combine large tables with additional tables
            var reservationTables = largeTables.Concat(additionalTables).ToList();

            int finalReservationCapacity = reservationTables.Sum(t => t.Capacity);
            double actualReservationPercentage = (double)finalReservationCapacity / totalCapacity * 100;

            _logger.LogInformation("Final reservation allocation: {TableCount} tables with {Capacity} capacity ({ActualPercent}% vs target {TargetPercent}%)",
                reservationTables.Count, finalReservationCapacity, Math.Round(actualReservationPercentage, 1), reservationAllocationPercent);

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

            // If allocation is 0%, all tables are for queue
            if (reservationAllocationPercent <= 0)
            {
                _logger.LogInformation("Reservation allocation is 0% for outlet {OutletId} at {DateTime}, returning all tables for queue",
                    outletId, dateTime);
                return activeTables;
            }

            // Get reservation tables first
            var reservationTables = await GetReservationTablesAsync(outletId, dateTime);

            // Filter out the reservation tables to get available tables for queue
            var reservationTableIds = reservationTables.Select(t => t.Id).ToHashSet();
            var queueTables = activeTables.Where(t => !reservationTableIds.Contains(t.Id)).ToList();

            int reservationCapacity = reservationTables.Sum(t => t.Capacity);
            int queueCapacity = queueTables.Sum(t => t.Capacity);
            int totalCapacity = reservationCapacity + queueCapacity;
            double queuePercent = (double)queueCapacity / totalCapacity * 100;
            double reservationPercent = (double)reservationCapacity / totalCapacity * 100;

            _logger.LogInformation("Found {Count} tables available for queue with capacity {Capacity} ({QueuePercent}%), " +
                "reservation tables: {ReservationCount} with capacity {ReservationCapacity} ({ReservationPercent}%)",
                queueTables.Count, queueCapacity, Math.Round(queuePercent, 1),
                reservationTables.Count, reservationCapacity, Math.Round(reservationPercent, 1));

            // Verify no large tables are in queue
            var largeTablesInQueue = queueTables.Where(t => t.Capacity > LARGE_TABLE_THRESHOLD).ToList();
            if (largeTablesInQueue.Any())
            {
                _logger.LogWarning("Warning: {Count} large tables were assigned to queue. This should not happen.",
                    largeTablesInQueue.Count);
                // This should never happen with our implementation, but logging as a safeguard
            }

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

        // Helper method to allocate additional tables for reservation
        private List<TableDto> AllocateAdditionalTables(List<TableDto> tables, int targetCapacity)
        {
            if (targetCapacity <= 0)
                return new List<TableDto>();

            // Sort by descending capacity (largest first, but all <= LARGE_TABLE_THRESHOLD)
            var sortedTables = tables.OrderByDescending(t => t.Capacity).ToList();
            var selectedTables = new List<TableDto>();
            int allocatedCapacity = 0;

            foreach (var table in sortedTables)
            {
                selectedTables.Add(table);
                allocatedCapacity += table.Capacity;

                // Stop when we've reached or exceeded the target
                if (allocatedCapacity >= targetCapacity)
                    break;
            }

            _logger.LogInformation("Allocated {Count} additional tables with total capacity {Capacity} (target: {Target})",
                selectedTables.Count, allocatedCapacity, targetCapacity);

            return selectedTables;
        }
    }
}