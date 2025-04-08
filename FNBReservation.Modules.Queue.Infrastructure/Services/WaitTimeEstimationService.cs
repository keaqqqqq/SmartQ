using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Queue.Infrastructure.Services
{
    public class WaitTimeEstimationService : IWaitTimeEstimationService
    {
        private readonly IQueueRepository _queueRepository;
        private readonly ITableService _tableService;
        private readonly ILogger<WaitTimeEstimationService> _logger;
        private readonly Dictionary<Guid, Dictionary<int, int>> _averageWaitTimesByOutlet;
        private readonly Dictionary<Guid, DateTime> _lastModelUpdateTime;
        private readonly ITableTypeService _tableTypeService; // Add this


        public WaitTimeEstimationService(
            IQueueRepository queueRepository,
            ITableService tableService,
            ITableTypeService tableTypeService, // Add this
            ILogger<WaitTimeEstimationService> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _tableTypeService = tableTypeService ?? throw new ArgumentNullException(nameof(tableTypeService)); // Add this
            _averageWaitTimesByOutlet = new Dictionary<Guid, Dictionary<int, int>>();
            _lastModelUpdateTime = new Dictionary<Guid, DateTime>();
        }

        public async Task<int> EstimateWaitTimeAsync(Guid outletId, int partySize, int queuePosition)
        {
            _logger.LogInformation("Estimating wait time for outlet {OutletId}, party size {PartySize}, position {Position}",
                outletId, partySize, queuePosition);

            try
            {
                // Check if model needs to be updated (train every hour)
                if (!_lastModelUpdateTime.ContainsKey(outletId) ||
                    DateTime.UtcNow - _lastModelUpdateTime[outletId] > TimeSpan.FromHours(1))
                {
                    await TrainModelAsync(outletId);
                }

                // Get current active customers count (only those in Waiting status, not Called)
                int activeWaitingCount = await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Waiting");

                // Calculate the effective queue position - this is more important than the absolute position
                // If a customer is in position 5 but there are only 3 customers ahead in Waiting status,
                // their effective position is 3
                int effectivePosition = Math.Min(queuePosition, activeWaitingCount);

                // Calculate base wait time using effective position
                double baseWaitMinutes = effectivePosition * 8; // Reduced from 10 to 8 minutes per queue position

                // For position 1, estimate should be much lower if customers ahead are already Called
                if (queuePosition == 1 || activeWaitingCount == 0)
                {
                    baseWaitMinutes = Math.Max(2, baseWaitMinutes / 2);
                }

                // Apply party size adjustment
                double partySizeMultiplier = GetPartySizeMultiplier(partySize);

                // Get table availability factor - more important now
                double tableAvailabilityFactor = await GetTableAvailabilityFactorAsync(outletId, partySize);

                if (effectivePosition <= 3 && tableAvailabilityFactor < 1.0)
                {
                    // Immediate seating possible
                    return effectivePosition <= 1 ? 5 : 10;
                }

                // Reduce the wait time based on Called customers
                int calledCount = await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Called");
                double calledFactor = Math.Max(0.5, 1.0 - (calledCount * 0.1)); // Each Called customer reduces wait by up to 10%

                // Use trained model data if available
                if (_averageWaitTimesByOutlet.ContainsKey(outletId))
                {
                    var waitTimesByPartySize = _averageWaitTimesByOutlet[outletId];
                    if (waitTimesByPartySize.ContainsKey(partySize))
                    {
                        // Use historical data as a weight
                        baseWaitMinutes = (baseWaitMinutes + waitTimesByPartySize[partySize]) / 2;
                    }
                }

                // Final calculation with all factors
                double estimatedWaitMinutes = baseWaitMinutes * partySizeMultiplier * tableAvailabilityFactor * calledFactor;

                // Apply minimum and maximum bounds
                estimatedWaitMinutes = Math.Max(5, Math.Min(estimatedWaitMinutes, 180));

                // If the customer is next in line and there's at least one available table, reduce the time dramatically
                if (effectivePosition == 1 && tableAvailabilityFactor < 1.2)
                {
                    estimatedWaitMinutes = Math.Min(estimatedWaitMinutes, 10);
                }

                return (int)Math.Ceiling(estimatedWaitMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating wait time for outlet {OutletId}", outletId);
                return queuePosition * 5; // Fallback to simpler estimation
            }
        }

        public async Task TrainModelAsync(Guid outletId)
        {
            _logger.LogInformation("Training wait time model for outlet {OutletId}", outletId);

            try
            {
                // Get historical data for completed queue entries in the last week
                var activeEntries = await _queueRepository.GetByOutletIdAsync(outletId, "Completed");
                var recentEntries = activeEntries
                    .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value >= DateTime.UtcNow.AddDays(-7))
                    .ToList();

                if (recentEntries.Count < 10)
                {
                    _logger.LogInformation("Not enough historical data for outlet {OutletId}. Need at least 10 entries, got {Count}",
                        outletId, recentEntries.Count);
                    return;
                }

                // Group by party size and calculate average wait time
                var waitTimesByPartySize = recentEntries
                    .Where(e => e.QueuedAt <= e.SeatedAt) // Ensure valid data points
                    .GroupBy(e => e.PartySize)
                    .ToDictionary(
                        g => g.Key,
                        g => (int)Math.Round(g.Average(e => (e.SeatedAt.Value - e.QueuedAt).TotalMinutes))
                    );

                // Update model
                _averageWaitTimesByOutlet[outletId] = waitTimesByPartySize;
                _lastModelUpdateTime[outletId] = DateTime.UtcNow;

                _logger.LogInformation("Wait time model trained for outlet {OutletId} with {Count} party size groups",
                    outletId, waitTimesByPartySize.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training wait time model for outlet {OutletId}", outletId);
            }
        }

        public async Task<Dictionary<int, int>> GetAverageWaitTimesByPartySizeAsync(Guid outletId)
        {
            if (!_averageWaitTimesByOutlet.ContainsKey(outletId))
            {
                await TrainModelAsync(outletId);
            }

            return _averageWaitTimesByOutlet.ContainsKey(outletId)
                ? _averageWaitTimesByOutlet[outletId]
                : new Dictionary<int, int>();
        }

        #region Helper Methods
        private double GetPartySizeMultiplier(int partySize)
        {
            // Larger parties typically wait longer
            if (partySize <= 2) return 0.8;
            if (partySize <= 4) return 1.0;
            if (partySize <= 6) return 1.2;
            if (partySize <= 8) return 1.5;
            return 1.8; // For very large parties
        }

        private async Task<double> GetTableAvailabilityFactorAsync(Guid outletId, int partySize)
        {
            try
            {
                // Get all tables for this outlet
                var allTables = await _tableService.GetTablesByOutletIdAsync(outletId);

                // Filter for queue tables only
                var queueTables = new List<TableDto>();
                foreach (var t in allTables)
                {
                    bool isQueueTable = await _tableTypeService.IsQueueTableAsync(outletId, t.Id, DateTime.UtcNow);
                    if (isQueueTable)
                    {
                        queueTables.Add(t);
                    }
                }

                // If no queue tables are found, log warning and fall back to all tables
                if (!queueTables.Any())
                {
                    _logger.LogWarning("No queue tables found for outlet {OutletId}. Using all available tables for wait time estimation.",
                        outletId);
                    queueTables = allTables.ToList();
                }

                // Count queue tables that can accommodate this party size
                int suitableTables = queueTables.Count(t => t.IsActive && t.Capacity >= partySize);

                // Since TableDto doesn't have a Status property yet, we'll use the number of
                // queue entries with "Called" or "Seated" status as a proxy for occupied tables
                int occupiedTablesCount = await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Called");
                occupiedTablesCount += await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Seated");

                // Estimate available tables by subtracting occupied tables from suitable tables
                int estimatedAvailableTables = Math.Max(0, suitableTables - occupiedTablesCount);

                // Calculate factor based on available tables
                if (estimatedAvailableTables <= 0) return 1.5; // No suitable tables available = longer wait
                if (estimatedAvailableTables == 1) return 1.2; // Only one suitable table
                if (estimatedAvailableTables <= 3) return 1.0; // A few suitable tables
                return 0.8; // Many suitable tables = shorter wait
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating table availability factor for outlet {OutletId}", outletId);
                return 1.0; // Default factor
            }
        }
        #endregion
    }
}