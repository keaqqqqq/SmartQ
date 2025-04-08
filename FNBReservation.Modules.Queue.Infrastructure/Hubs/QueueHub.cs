// FNBReservation.Modules.Queue.Infrastructure/Hubs/QueueHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Modules.Queue.Infrastructure.Hubs
{
    public class QueueHub : Hub
    {
        private readonly ILogger<QueueHub> _logger;
        private static readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        public QueueHub(ILogger<QueueHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected with ID: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

            // Remove connection from tracking dictionary
            foreach (var kvp in _userConnections.ToList())
            {
                if (kvp.Value == Context.ConnectionId)
                {
                    _userConnections.Remove(kvp.Key);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinOutletGroup(string outletId)
        {
            try
            {
                _logger.LogInformation("Client {ConnectionId} is trying to join outlet group: {OutletId}",
                    Context.ConnectionId, outletId);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"outlet-{outletId}");

                _logger.LogInformation("Client {ConnectionId} successfully joined outlet group: {OutletId}",
                    Context.ConnectionId, outletId);

                await Clients.Caller.SendAsync("JoinedGroup", outletId);

                _logger.LogInformation("Sent JoinedGroup confirmation to client {ConnectionId}",
                    Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinOutletGroup for client {ConnectionId}, outlet {OutletId}",
                    Context.ConnectionId, outletId);
                throw;
            }
        }

        public async Task LeaveOutletGroup(string outletId)
        {
            _logger.LogInformation("Client {ConnectionId} leaving outlet group: {OutletId}", Context.ConnectionId, outletId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
        }

        public async Task RegisterQueueEntry(string queueCode)
        {
            _logger.LogInformation("Registering connection {ConnectionId} for queue code: {QueueCode}", Context.ConnectionId, queueCode);

            // Store the connection ID for this queue code
            _userConnections[queueCode] = Context.ConnectionId;

            // Add to a group for the queue code
            await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueCode}");
        }

        public async Task UnregisterQueueEntry(string queueCode)
        {
            _logger.LogInformation("Unregistering connection for queue code: {QueueCode}", queueCode);

            // Remove from connections dictionary
            if (_userConnections.ContainsKey(queueCode))
            {
                _userConnections.Remove(queueCode);
            }

            // Remove from queue group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"queue-{queueCode}");
        }
    }
}