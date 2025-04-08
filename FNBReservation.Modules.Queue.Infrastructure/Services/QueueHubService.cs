using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.DTOs;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Queue.Infrastructure.Hubs;

namespace FNBReservation.Modules.Queue.Infrastructure.Services
{
    public class QueueHubService : IQueueHub
    {
        private readonly IHubContext<QueueHub> _hubContext;
        private readonly ILogger<QueueHubService> _logger;

        public QueueHubService(
            IHubContext<QueueHub> hubContext,
            ILogger<QueueHubService> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task NotifyQueueUpdated(Guid outletId)
        {
            _logger.LogInformation("Notifying queue updated for outlet: {OutletId}", outletId);
            await _hubContext.Clients.Group($"outlet-{outletId}").SendAsync("QueueUpdated");
        }

        public async Task UpdateQueueStatus(QueueStatusDto queueStatusUpdate)
        {
            _logger.LogInformation("Updating queue status for entry: {QueueEntryId}", queueStatusUpdate.QueueEntryId);

            // Send to the specific queue code group
            await _hubContext.Clients.Group($"queue-{queueStatusUpdate.QueueCode}").SendAsync("StatusUpdated", queueStatusUpdate);

            // Note: We can't directly check connections dictionary since that's in the Hub
            // Instead, we send to all clients connected to this queue code
        }

        public async Task NotifyTableReady(Guid queueEntryId, string tableNumber)
        {
            _logger.LogInformation("Notifying table ready for queue entry: {QueueEntryId}, table: {TableNumber}",
                queueEntryId, tableNumber);

            // This will be sent to the specific queue entry
            await _hubContext.Clients.Group($"queue-{queueEntryId}").SendAsync("TableReady", new
            {
                queueEntryId = queueEntryId,
                tableNumber = tableNumber,
                message = $"Your table {tableNumber} is ready! Please proceed to the host stand."
            });
        }
    }
}