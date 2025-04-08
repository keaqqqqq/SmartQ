// Updated QueueNotificationService.cs
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FNBReservation.Modules.Queue.Core.Entities;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;
// Import from our new shared notification module
using FNBReservation.Modules.Notification.Core.Interfaces;

namespace FNBReservation.Modules.Queue.Infrastructure.Services
{
    public class QueueNotificationService : IQueueNotificationService
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IOutletService _outletService;
        private readonly ILogger<QueueNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public QueueNotificationService(
            IQueueRepository queueRepository,
            IWhatsAppService whatsAppService,
            IOutletService outletService,
            ILogger<QueueNotificationService> logger,
            IConfiguration configuration)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _whatsAppService = whatsAppService ?? throw new ArgumentNullException(nameof(whatsAppService));
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendQueueConfirmationAsync(Guid queueEntryId)
        {
            _logger.LogInformation("Sending queue confirmation for entry: {QueueEntryId}", queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    return;
                }

                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", queueEntry.OutletId);
                    return;
                }

                // Log the notification in the database first
                var notification = new QueueNotification
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntryId,
                    NotificationType = "Confirmation",
                    Channel = "WhatsApp",
                    Content = "Queue confirmation notification",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _queueRepository.AddNotificationAsync(notification);

                // Determine which method to use based on configuration
                var useTemplates = _configuration.GetValue<bool>("WhatsAppApi:UseTemplates", false);

                if (useTemplates)
                {
                    // Use template-based messaging
                    var templateParams = new List<object>
                    {
                        queueEntry.CustomerName,
                        outlet.Name,
                        queueEntry.QueueCode,
                        queueEntry.QueuePosition.ToString(),
                        queueEntry.EstimatedWaitMinutes.ToString(),
                        queueEntry.PartySize.ToString(),
                        !string.IsNullOrEmpty(queueEntry.SpecialRequests) ?
                            queueEntry.SpecialRequests : "None",
                        queueEntry.QueueCode,
                        DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        outlet.Contact
                    };

                    // Send using template
                    await _whatsAppService.SendTemplateMessageAsync(
                        queueEntry.CustomerPhone,
                        "queue_confirmation",
                        templateParams
                    );

                    notification.Content = "Template: queue_confirmation";
                }
                else
                {
                    // Use regular text-based messaging
                    var message = BuildQueueConfirmationMessage(queueEntry, outlet.Name);
                    await _whatsAppService.SendMessageAsync(queueEntry.CustomerPhone, message);
                    notification.Content = message;
                }

                // Update notification status
                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                await _queueRepository.UpdateNotificationAsync(notification);

                _logger.LogInformation("Queue confirmation sent for entry: {QueueEntryId}", queueEntryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue confirmation for entry: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task SendTableReadyNotificationAsync(Guid queueEntryId)
        {
            _logger.LogInformation("Sending table ready notification for entry: {QueueEntryId}", queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    return;
                }

                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", queueEntry.OutletId);
                    return;
                }

                // Get the assigned table number(s)
                var tableNumbers = string.Join(", ", queueEntry.TableAssignments.Select(ta => ta.TableNumber));

                // Log the notification in the database
                var notification = new QueueNotification
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntryId,
                    NotificationType = "TableReady",
                    Channel = "WhatsApp",
                    Content = "Table ready notification",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _queueRepository.AddNotificationAsync(notification);

                // Determine which method to use based on configuration
                var useTemplates = _configuration.GetValue<bool>("WhatsAppApi:UseTemplates", false);

                if (useTemplates)
                {
                    // Use template-based messaging
                    var templateParams = new List<object>
                    {
                        queueEntry.CustomerName,
                        outlet.Name,
                        queueEntry.QueueCode,
                        tableNumbers
                    };

                    // Send using template
                    await _whatsAppService.SendTemplateMessageAsync(
                        queueEntry.CustomerPhone,
                        "table_ready",
                        templateParams
                    );

                    notification.Content = "Template: table_ready";
                }
                else
                {
                    // Use regular text-based messaging
                    var message = BuildTableReadyMessage(queueEntry, outlet.Name, tableNumbers);
                    await _whatsAppService.SendMessageAsync(queueEntry.CustomerPhone, message);
                    notification.Content = message;
                }

                // Update notification status
                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                await _queueRepository.UpdateNotificationAsync(notification);

                _logger.LogInformation("Table ready notification sent for entry: {QueueEntryId}", queueEntryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending table ready notification for entry: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task SendQueueUpdateAsync(Guid queueEntryId)
        {
            _logger.LogInformation("Sending queue update for entry: {QueueEntryId}", queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    return;
                }

                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", queueEntry.OutletId);
                    return;
                }

                // Log the notification in the database
                var notification = new QueueNotification
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntryId,
                    NotificationType = "Update",
                    Channel = "WhatsApp",
                    Content = "Queue position update notification",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _queueRepository.AddNotificationAsync(notification);

                // Determine which method to use based on configuration
                var useTemplates = _configuration.GetValue<bool>("WhatsAppApi:UseTemplates", false);

                if (useTemplates)
                {
                    // Use template-based messaging
                    var templateParams = new List<object>
                    {
                        queueEntry.CustomerName,
                        queueEntry.QueueCode,
                        queueEntry.QueuePosition.ToString(),
                        queueEntry.EstimatedWaitMinutes.ToString(),
                        queueEntry.QueueCode,
                        DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        outlet.Contact
                    };

                    // Send using template
                    await _whatsAppService.SendTemplateMessageAsync(
                        queueEntry.CustomerPhone,
                        "queue_update_new",
                        templateParams
                    );

                    notification.Content = "Template: queue_update";
                }
                else
                {
                    // Use regular text-based messaging
                    var message = BuildQueueUpdateMessage(queueEntry, outlet.Name);
                    await _whatsAppService.SendMessageAsync(queueEntry.CustomerPhone, message);
                    notification.Content = message;
                }

                // Update notification status
                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                await _queueRepository.UpdateNotificationAsync(notification);

                _logger.LogInformation("Queue update sent for entry: {QueueEntryId}", queueEntryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue update for entry: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task SendQueueCancellationAsync(Guid queueEntryId, string reason)
        {
            _logger.LogInformation("Sending queue cancellation for entry: {QueueEntryId}", queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    return;
                }

                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", queueEntry.OutletId);
                    return;
                }

                // Log the notification in the database
                var notification = new QueueNotification
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntryId,
                    NotificationType = "Cancellation",
                    Channel = "WhatsApp",
                    Content = "Queue cancellation notification",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _queueRepository.AddNotificationAsync(notification);

                // Determine which method to use based on configuration
                var useTemplates = _configuration.GetValue<bool>("WhatsAppApi:UseTemplates", false);

                if (useTemplates)
                {
                    // Use template-based messaging
                    var templateParams = new List<object>
                    {
                        queueEntry.CustomerName,
                        outlet.Name,
                        queueEntry.QueueCode,
                        reason ?? "Not specified"
                    };

                    // Send using template
                    await _whatsAppService.SendTemplateMessageAsync(
                        queueEntry.CustomerPhone,
                        "queue_cancellation",
                        templateParams
                    );

                    notification.Content = "Template: queue_cancellation";
                }
                else
                {
                    // Use regular text-based messaging
                    var message = BuildQueueCancellationMessage(queueEntry, outlet.Name, reason);
                    await _whatsAppService.SendMessageAsync(queueEntry.CustomerPhone, message);
                    notification.Content = message;
                }

                // Update notification status
                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                await _queueRepository.UpdateNotificationAsync(notification);

                _logger.LogInformation("Queue cancellation sent for entry: {QueueEntryId}", queueEntryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue cancellation for entry: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        #region Helper Methods (Keep these for fallback to text messaging)
        private string BuildQueueConfirmationMessage(QueueEntry queueEntry, string outletName)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {queueEntry.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"You have been added to the queue at {outletName}!");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Queue Details:");
            messageBuilder.AppendLine($"- Queue Code: {queueEntry.QueueCode}");
            messageBuilder.AppendLine($"- Position: {queueEntry.QueuePosition}");
            messageBuilder.AppendLine($"- Estimated Wait Time: {queueEntry.EstimatedWaitMinutes} minutes");
            messageBuilder.AppendLine($"- Party Size: {queueEntry.PartySize}");

            if (!string.IsNullOrEmpty(queueEntry.SpecialRequests))
            {
                messageBuilder.AppendLine($"- Special Requests: {queueEntry.SpecialRequests}");
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We'll send you updates as your position changes.");
            messageBuilder.AppendLine("If you need to leave the queue, please update through the app.");

            return messageBuilder.ToString();
        }

        private string BuildTableReadyMessage(QueueEntry queueEntry, string outletName, string tableNumbers)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {queueEntry.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Your table at {outletName} is ready!");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Please proceed to the host stand and mention your queue code: {queueEntry.QueueCode}");

            if (!string.IsNullOrEmpty(tableNumbers))
            {
                messageBuilder.AppendLine($"You've been assigned to table(s): {tableNumbers}");
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Please arrive within the next 10 minutes to keep your table.");
            messageBuilder.AppendLine("We look forward to serving you!");

            return messageBuilder.ToString();
        }

        private string BuildQueueUpdateMessage(QueueEntry queueEntry, string outletName)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {queueEntry.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Here's an update on your position in the queue at {outletName}:");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"- Queue Code: {queueEntry.QueueCode}");
            messageBuilder.AppendLine($"- Updated Position: {queueEntry.QueuePosition}");
            messageBuilder.AppendLine($"- Estimated Wait Time: {queueEntry.EstimatedWaitMinutes} minutes");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We'll notify you when your table is ready.");
            messageBuilder.AppendLine("Thanks for your patience!");

            return messageBuilder.ToString();
        }

        private string BuildQueueCancellationMessage(QueueEntry queueEntry, string outletName, string reason)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {queueEntry.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Your place in the queue at {outletName} has been cancelled.");
            messageBuilder.AppendLine();

            if (!string.IsNullOrEmpty(reason))
            {
                messageBuilder.AppendLine($"Reason: {reason}");
                messageBuilder.AppendLine();
            }

            messageBuilder.AppendLine("If you'd like to rejoin the queue, please scan the QR code at the restaurant entrance.");
            messageBuilder.AppendLine("We hope to welcome you back soon!");

            return messageBuilder.ToString();
        }
        #endregion
    }
}