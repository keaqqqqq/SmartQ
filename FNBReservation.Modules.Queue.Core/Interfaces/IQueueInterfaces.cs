using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Queue.Core.DTOs;
using FNBReservation.Modules.Queue.Core.Entities;

namespace FNBReservation.Modules.Queue.Core.Interfaces
{
    public interface IQueueService
    {
        Task<QueueEntryDto> CreateQueueEntryAsync(CreateQueueEntryDto createQueueEntryDto);
        Task<QueueEntryDto> GetQueueEntryByIdAsync(Guid id);
        Task<QueueEntryDto> GetQueueEntryByCodeAsync(string queueCode);
        Task<IEnumerable<QueueEntryDto>> GetQueueEntriesByOutletIdAsync(Guid outletId, string status = null);
        Task<QueueEntryDto> UpdateQueueEntryAsync(Guid id, UpdateQueueEntryDto updateQueueEntryDto);
        Task<QueueEntryDto> UpdateQueueStatusAsync(Guid id, QueueStatusUpdateDto statusUpdateDto, Guid? staffId = null);
        Task<bool> CancelQueueEntryAsync(Guid id, string reason, Guid? staffId = null);
        Task<QueueEntryDto> AssignTableToQueueEntryAsync(Guid queueEntryId, AssignTableDto assignTableDto);
        Task<QueueEntryDto> MarkQueueEntryAsSeatedAsync(Guid queueEntryId, Guid staffId);
        Task<QueueEntryDto> MarkQueueEntryAsCompletedAsync(Guid queueEntryId, Guid staffId);
        Task<QueueEntryDto> MarkQueueEntryAsNoShowAsync(Guid queueEntryId, Guid staffId);
        Task<QueueEntryListResponseDto> GetQueueEntriesAsync(
            Guid outletId,
            List<string> statuses = null,
            string searchTerm = null,
            int page = 1,
            int pageSize = 20);
        Task<QueueSummaryDto> GetQueueSummaryAsync(Guid outletId);
        Task<int> GetEstimatedWaitTimeAsync(Guid outletId, int partySize);
        Task<QueueEntryDto> CallNextCustomerAsync(Guid outletId, Guid tableId, Guid staffId);
        Task<TableRecommendationDto> GetTableRecommendationAsync(Guid outletId, Guid tableId);
        Task<List<QueueEntryDto>> GetHeldEntriesAsync(Guid outletId);
        Task<QueueEntryDto> PrioritizeHeldEntryAsync(Guid queueEntryId, Guid staffId);
        Task ReorderQueueAsync(Guid outletId, Guid? processedQueueEntryId = null);
        Task UpdateWaitTimesAsync(Guid outletId);
    }

    public interface IQueueRepository
    {
        Task<QueueEntry> CreateAsync(QueueEntry queueEntry);
        Task<QueueEntry> GetByIdAsync(Guid id);
        Task<QueueEntry> GetByCodeAsync(string queueCode);
        Task<IEnumerable<QueueEntry>> GetByOutletIdAsync(Guid outletId, string status = null);
        Task<QueueEntry> UpdateAsync(QueueEntry queueEntry);
        Task<bool> DeleteAsync(Guid id);
        Task AddStatusChangeAsync(QueueStatusChange statusChange);
        Task AddTableAssignmentAsync(QueueTableAssignment tableAssignment);
        Task UpdateTableAssignmentAsync(QueueTableAssignment tableAssignment);
        Task AddNotificationAsync(QueueNotification notification);
        Task UpdateNotificationAsync(QueueNotification notification);
        Task<int> GetQueuePositionAsync(Guid outletId, Guid queueEntryId);
        Task<int> CountActiveQueueEntriesAsync(Guid outletId);
        Task<int> CountQueueEntriesByStatusAsync(Guid outletId, string status);
        Task<int> GetLongestWaitTimeAsync(Guid outletId);
        Task<int> GetAverageWaitTimeAsync(Guid outletId);
        Task<IEnumerable<QueueEntry>> GetActiveQueueEntriesByPartySize(Guid outletId, int minPartySize, int maxPartySize);
        Task<IEnumerable<QueueEntry>> GetHeldEntriesAsync(Guid outletId);
        Task<double> GetAverageSeatingDurationAsync(Guid outletId, int partySize);
        Task<(List<QueueEntry> Entries, int TotalCount)> SearchEntriesAsync(
            Guid outletId,
            List<string> statuses,
            string searchTerm,
            int page,
            int pageSize);

        Task<IEnumerable<QueueTableAssignment>> GetActiveTableAssignmentsAsync(Guid tableId);
    }

    public interface IQueueNotificationService
    {
        Task SendQueueConfirmationAsync(Guid queueEntryId);
        Task SendTableReadyNotificationAsync(Guid queueEntryId);
        Task SendQueueUpdateAsync(Guid queueEntryId);
        Task SendQueueCancellationAsync(Guid queueEntryId, string reason);
    }

    public interface IWaitTimeEstimationService
    {
        Task<int> EstimateWaitTimeAsync(Guid outletId, int partySize, int queuePosition);
        Task TrainModelAsync(Guid outletId);
        Task<Dictionary<int, int>> GetAverageWaitTimesByPartySizeAsync(Guid outletId);
    }

    // Interface for Queue Hub that will handle WebSocket connections
    public interface IQueueHub
    {
        Task NotifyQueueUpdated(Guid outletId);
        Task UpdateQueueStatus(QueueStatusDto queueStatusUpdate);
        Task NotifyTableReady(Guid queueEntryId, string tableNumber);
    }
}