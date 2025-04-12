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

            // If allocation is 100%, all tables are for reservations
            if (reservationAllocationPercent >= 100)
            {
                _logger.LogInformation("Reservation allocation is 100% for outlet {OutletId} at {DateTime}, returning all tables",
                    outletId, dateTime);
                return activeTables;
            }

            // Calculate CAPACITY-based allocation (not table count)
            int totalCapacity = activeTables.Sum(t => t.Capacity);
            int targetReservationCapacity = (int)Math.Ceiling(totalCapacity * (reservationAllocationPercent / 100.0));

            _logger.LogInformation("Target reservation capacity: {Target} out of {Total} total capacity ({Percent}%)",
                targetReservationCapacity, totalCapacity, reservationAllocationPercent);

            // Use a capacity-based allocation algorithm
            var reservationTables = AllocateTablesByCapacity(activeTables, targetReservationCapacity);

            _logger.LogInformation("Selected {Count} tables for reservations with total capacity {Capacity} out of {Total} total capacity",
                reservationTables.Count, reservationTables.Sum(t => t.Capacity), totalCapacity);

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

            _logger.LogInformation("Found {Count} tables available for queue with capacity {Capacity} ({QueuePercent}%), " +
                "reservation tables: {ReservationCount} with capacity {ReservationCapacity} ({ReservationPercent}%)",
                queueTables.Count, queueCapacity, Math.Round(100.0 * queueCapacity / totalCapacity, 1),
                reservationTables.Count, reservationCapacity, Math.Round(100.0 * reservationCapacity / totalCapacity, 1));

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

        // Helper method to allocate tables based on capacity target
        private List<TableDto> AllocateTablesByCapacity(List<TableDto> allTables, int targetCapacity)
        {
            // Try multiple approaches and select the one that gives closest to target capacity

            // Approach 1: Start with largest tables with better-fit optimization
            var largeFirstResult = new List<TableDto>();
            int largeFirstCapacity = 0;
            var largeFirstRemainingTables = new List<TableDto>(allTables);

            // Sort by largest capacity first
            largeFirstRemainingTables.Sort((a, b) => b.Capacity.CompareTo(a.Capacity));

            while (largeFirstRemainingTables.Any() && largeFirstCapacity < targetCapacity)
            {
                var currentTable = largeFirstRemainingTables[0];
                largeFirstRemainingTables.RemoveAt(0);

                // If adding this table would exceed target by too much, try to find a better fit
                if (largeFirstCapacity + currentTable.Capacity > targetCapacity * 1.1)
                {
                    // Look for a smaller table that fits better
                    var betterFit = largeFirstRemainingTables
                        .Where(t => largeFirstCapacity + t.Capacity <= targetCapacity)
                        .OrderByDescending(t => t.Capacity)
                        .FirstOrDefault();

                    if (betterFit != null)
                    {
                        largeFirstResult.Add(betterFit);
                        largeFirstCapacity += betterFit.Capacity;
                        largeFirstRemainingTables.Remove(betterFit);
                        continue;
                    }
                }

                // If we're still below target or couldn't find a better fit, add current table
                largeFirstResult.Add(currentTable);
                largeFirstCapacity += currentTable.Capacity;

                // Stop if we've reached or exceeded target
                if (largeFirstCapacity >= targetCapacity)
                    break;
            }

            // Approach 2: Start with smallest tables
            var smallFirstResult = new List<TableDto>();
            int smallFirstCapacity = 0;

            foreach (var table in allTables.OrderBy(t => t.Capacity))
            {
                smallFirstResult.Add(table);
                smallFirstCapacity += table.Capacity;

                // Stop if we've reached or exceeded target
                if (smallFirstCapacity >= targetCapacity)
                    break;
            }

            // Approach 3: Bin packing algorithm (using greedy approximation)
            var binPackResult = BinPackingAllocation(allTables, targetCapacity);
            int binPackCapacity = binPackResult.Sum(t => t.Capacity);

            // Choose the approach that gives closest to target capacity
            int largeFirstDiff = Math.Abs(largeFirstCapacity - targetCapacity);
            int smallFirstDiff = Math.Abs(smallFirstCapacity - targetCapacity);
            int binPackDiff = Math.Abs(binPackCapacity - targetCapacity);

            _logger.LogInformation("Allocation options - LargeFirst: {LargeCapacity}, SmallFirst: {SmallCapacity}, BinPack: {BinCapacity}, Target: {Target}",
                largeFirstCapacity, smallFirstCapacity, binPackCapacity, targetCapacity);

            if (largeFirstDiff <= smallFirstDiff && largeFirstDiff <= binPackDiff)
            {
                _logger.LogInformation("Using large-first approach. Allocated capacity: {Capacity}", largeFirstCapacity);
                return largeFirstResult;
            }
            else if (smallFirstDiff <= binPackDiff)
            {
                _logger.LogInformation("Using small-first approach. Allocated capacity: {Capacity}", smallFirstCapacity);
                return smallFirstResult;
            }
            else
            {
                _logger.LogInformation("Using bin-packing approach. Allocated capacity: {Capacity}", binPackCapacity);
                return binPackResult;
            }
        }

        // Bin packing algorithm implementation (simple greedy approximation)
        private List<TableDto> BinPackingAllocation(List<TableDto> tables, int targetCapacity)
        {
            // Sort tables by decreasing capacity
            var sortedTables = tables.OrderByDescending(t => t.Capacity).ToList();
            var selectedTables = new List<TableDto>();
            var remainingCapacity = targetCapacity;

            // First pass: try to fill in tables that fit exactly or nearly
            for (int i = 0; i < sortedTables.Count; i++)
            {
                // If we've filled the bin, break
                if (remainingCapacity <= 0)
                    break;

                var table = sortedTables[i];

                // If this table fits perfectly or nearly perfectly
                if (table.Capacity <= remainingCapacity &&
                    (table.Capacity > remainingCapacity * 0.9 || remainingCapacity - table.Capacity < 3))
                {
                    selectedTables.Add(table);
                    remainingCapacity -= table.Capacity;
                    sortedTables.RemoveAt(i);
                    i--; // Adjust index since we removed an item
                }
            }

            // Second pass: first-fit-decreasing approach for remaining space
            foreach (var table in sortedTables)
            {
                if (table.Capacity <= remainingCapacity)
                {
                    selectedTables.Add(table);
                    remainingCapacity -= table.Capacity;

                    // If we've filled the bin enough, break
                    if (remainingCapacity <= 0)
                        break;
                }
            }

            // If we've selected nothing, at least pick the smallest table
            if (selectedTables.Count == 0 && tables.Any())
            {
                var smallestTable = tables.OrderBy(t => t.Capacity).First();
                selectedTables.Add(smallestTable);
            }

            return selectedTables;
        }
    }
}