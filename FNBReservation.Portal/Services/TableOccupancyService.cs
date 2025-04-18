using FNBReservation.Portal.Models;
using System.Collections.Concurrent;

namespace FNBReservation.Portal.Services
{
    public interface ITableOccupancyService
    {
        Dictionary<string, QueueEntryDto> GetQueueTableOccupancy(string outletId);
        void SetQueueTableOccupancy(string outletId, Dictionary<string, QueueEntryDto> occupancy);
        Dictionary<string, string> GetTableStatuses(string outletId);
        void SetTableStatuses(string outletId, Dictionary<string, string> statuses);
        void ClearOccupancyData(string outletId);
        void MarkTableAsOccupied(string outletId, string tableId, QueueEntryDto queueEntry);
        void MarkTableAsAvailable(string outletId, string tableId);
    }

    public class TableOccupancyService : ITableOccupancyService
    {
        // Store occupancy data per outlet
        private readonly ConcurrentDictionary<string, Dictionary<string, QueueEntryDto>> _queueTableOccupancyByOutlet = new();
        
        // Store table statuses per outlet
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _tableStatusesByOutlet = new();

        public Dictionary<string, QueueEntryDto> GetQueueTableOccupancy(string outletId)
        {
            return _queueTableOccupancyByOutlet.GetValueOrDefault(outletId, new Dictionary<string, QueueEntryDto>());
        }

        public void SetQueueTableOccupancy(string outletId, Dictionary<string, QueueEntryDto> occupancy)
        {
            _queueTableOccupancyByOutlet[outletId] = occupancy;
        }

        public Dictionary<string, string> GetTableStatuses(string outletId)
        {
            return _tableStatusesByOutlet.GetValueOrDefault(outletId, new Dictionary<string, string>());
        }

        public void SetTableStatuses(string outletId, Dictionary<string, string> statuses)
        {
            _tableStatusesByOutlet[outletId] = statuses;
        }

        public void ClearOccupancyData(string outletId)
        {
            _queueTableOccupancyByOutlet.TryRemove(outletId, out _);
            _tableStatusesByOutlet.TryRemove(outletId, out _);
        }

        public void MarkTableAsOccupied(string outletId, string tableId, QueueEntryDto queueEntry)
        {
            if (!_queueTableOccupancyByOutlet.ContainsKey(outletId))
            {
                _queueTableOccupancyByOutlet[outletId] = new Dictionary<string, QueueEntryDto>();
            }
            
            if (!_tableStatusesByOutlet.ContainsKey(outletId))
            {
                _tableStatusesByOutlet[outletId] = new Dictionary<string, string>();
            }
            
            _queueTableOccupancyByOutlet[outletId][tableId] = queueEntry;
            _tableStatusesByOutlet[outletId][tableId] = "occupied";
        }

        public void MarkTableAsAvailable(string outletId, string tableId)
        {
            if (_queueTableOccupancyByOutlet.TryGetValue(outletId, out var occupancy))
            {
                occupancy.Remove(tableId);
            }
            
            if (_tableStatusesByOutlet.TryGetValue(outletId, out var statuses))
            {
                statuses[tableId] = "available";
            }
        }
    }
} 