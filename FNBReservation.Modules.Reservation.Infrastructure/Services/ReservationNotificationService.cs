using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Reservation.Core.Entities;

namespace FNBReservation.Modules.Reservation.Infrastructure.Services
{
    public class ReservationNotificationService : IReservationNotificationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IOutletAdapter _outletAdapter;
        private readonly ILogger<ReservationNotificationService> _logger;
        private readonly IConfiguration _configuration;

        // This class would need to be replaced with an actual WhatsApp API integration
        private readonly IWhatsAppService _whatsAppService;

        public ReservationNotificationService(
            IReservationRepository reservationRepository,
            IOutletAdapter outletAdapter,
            IWhatsAppService whatsAppService,
            ILogger<ReservationNotificationService> logger,
            IConfiguration configuration)
        {
            _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
            _outletAdapter = outletAdapter ?? throw new ArgumentNullException(nameof(outletAdapter));
            _whatsAppService = whatsAppService ?? throw new ArgumentNullException(nameof(whatsAppService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendConfirmationAsync(Guid reservationId)
        {
            _logger.LogInformation("Sending confirmation for reservation: {ReservationId}", reservationId);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", reservationId);
                    return;
                }

                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                    return;
                }

                // Create confirmation message
                var message = BuildConfirmationMessage(reservation, outlet.Name);

                // Send the message first
                await _whatsAppService.SendMessageAsync(reservation.CustomerPhone, message);

                // Check if we already have this reminder - if this is called from ScheduleRemindersAsync
                // the reminder record might already exist
                var existingReminders = reservation.Reminders.Where(r => r.ReminderType == "Confirmation").ToList();

                if (!existingReminders.Any())
                {
                    // Only add a new reminder record if one doesn't already exist
                    var reminder = new ReservationReminder
                    {
                        Id = Guid.NewGuid(),
                        ReservationId = reservationId,
                        ReminderType = "Confirmation",
                        ScheduledFor = DateTime.UtcNow,
                        SentAt = DateTime.UtcNow,
                        Status = "Sent",
                        Channel = "WhatsApp",
                        Content = message
                    };

                    // Save the reminder
                    await _reservationRepository.AddReminderAsync(reminder);
                }
                else
                {
                    // Update existing reminder as sent
                    var reminder = existingReminders.First();
                    reminder.Status = "Sent";
                    reminder.SentAt = DateTime.UtcNow;
                    reminder.Content = message;
                    await _reservationRepository.UpdateReminderAsync(reminder);
                }

                _logger.LogInformation("Confirmation sent for reservation: {ReservationId}", reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation for reservation: {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task SendReminderAsync(Guid reservationId, string reminderType)
        {
            _logger.LogInformation("Sending {ReminderType} reminder for reservation: {ReservationId}", reminderType, reservationId);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", reservationId);
                    return;
                }

                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                    return;
                }

                // Create reminder message based on type
                string message = reminderType switch
                {
                    "24Hour" => Build24HourReminderMessage(reservation, outlet.Name),
                    "1Hour" => Build1HourReminderMessage(reservation, outlet.Name),
                    _ => BuildGenericReminderMessage(reservation, outlet.Name)
                };

                // Send the message
                await _whatsAppService.SendMessageAsync(reservation.CustomerPhone, message);

                // Check if we already have this reminder
                var existingReminders = reservation.Reminders.Where(r => r.ReminderType == reminderType).ToList();

                if (!existingReminders.Any())
                {
                    // Only add a new reminder record if one doesn't already exist
                    var reminder = new ReservationReminder
                    {
                        Id = Guid.NewGuid(),
                        ReservationId = reservationId,
                        ReminderType = reminderType,
                        ScheduledFor = DateTime.UtcNow,
                        SentAt = DateTime.UtcNow,
                        Status = "Sent",
                        Channel = "WhatsApp",
                        Content = message
                    };

                    // Save the reminder
                    await _reservationRepository.AddReminderAsync(reminder);
                }
                else
                {
                    // Update existing reminder as sent
                    var reminder = existingReminders.First();
                    reminder.Status = "Sent";
                    reminder.SentAt = DateTime.UtcNow;
                    reminder.Content = message;
                    await _reservationRepository.UpdateReminderAsync(reminder);
                }

                _logger.LogInformation("{ReminderType} reminder sent for reservation: {ReservationId}", reminderType, reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {ReminderType} reminder for reservation: {ReservationId}", reminderType, reservationId);
                throw;
            }
        }


        public async Task SendCancellationAsync(Guid reservationId, string reason)
        {
            _logger.LogInformation("Sending cancellation notification for reservation: {ReservationId}", reservationId);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", reservationId);
                    return;
                }

                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                    return;
                }

                // Create cancellation message
                var message = BuildCancellationMessage(reservation, outlet.Name, reason);

                // Log the notification in the database
                var reminder = new ReservationReminder
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservationId,
                    ReminderType = "Cancellation",
                    ScheduledFor = DateTime.UtcNow,
                    SentAt = DateTime.UtcNow,
                    Status = "Sent",
                    Channel = "WhatsApp",
                    Content = message
                };

                // Send the message
                await _whatsAppService.SendMessageAsync(reservation.CustomerPhone, message);

                // Save the notification
                await _reservationRepository.AddReminderAsync(reminder);

                _logger.LogInformation("Cancellation notification sent for reservation: {ReservationId}", reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cancellation notification for reservation: {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task SendModificationAsync(Guid reservationId, string changes)
        {
            _logger.LogInformation("Sending modification notification for reservation: {ReservationId}", reservationId);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", reservationId);
                    return;
                }

                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                    return;
                }

                // Create modification message
                var message = BuildModificationMessage(reservation, outlet.Name, changes);

                // Log the notification in the database
                var reminder = new ReservationReminder
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservationId,
                    ReminderType = "Modification",
                    ScheduledFor = DateTime.UtcNow,
                    SentAt = DateTime.UtcNow,
                    Status = "Sent",
                    Channel = "WhatsApp",
                    Content = message
                };

                // Send the message
                await _whatsAppService.SendMessageAsync(reservation.CustomerPhone, message);

                // Save the notification
                await _reservationRepository.AddReminderAsync(reminder);

                _logger.LogInformation("Modification notification sent for reservation: {ReservationId}", reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending modification notification for reservation: {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task ProcessPendingRemindersAsync()
        {
            _logger.LogInformation("Processing pending reminders");

            try
            {
                // Get all pending reminders scheduled before now
                var pendingReminders = await _reservationRepository.GetPendingRemindersAsync(DateTime.UtcNow);

                foreach (var reminder in pendingReminders)
                {
                    try
                    {
                        // Skip reminders for canceled reservations
                        if (reminder.Reservation.Status == "Canceled" || reminder.Reservation.Status == "NoShow")
                        {
                            reminder.Status = "Skipped";
                            await _reservationRepository.UpdateReminderAsync(reminder);
                            continue;
                        }

                        // Send the reminder based on its type
                        switch (reminder.ReminderType)
                        {
                            case "Confirmation":
                                await SendConfirmationAsync(reminder.ReservationId);
                                break;
                            case "24Hour":
                            case "1Hour":
                                await SendReminderAsync(reminder.ReservationId, reminder.ReminderType);
                                break;
                            default:
                                // Unknown reminder type, log and skip
                                _logger.LogWarning("Unknown reminder type: {ReminderType} for reservation: {ReservationId}",
                                    reminder.ReminderType, reminder.ReservationId);
                                reminder.Status = "Skipped";
                                await _reservationRepository.UpdateReminderAsync(reminder);
                                continue;
                        }

                        // Update reminder status
                        reminder.Status = "Sent";
                        reminder.SentAt = DateTime.UtcNow;
                        await _reservationRepository.UpdateReminderAsync(reminder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing reminder {ReminderId} for reservation {ReservationId}",
                            reminder.Id, reminder.ReservationId);

                        // Mark as failed
                        reminder.Status = "Failed";
                        await _reservationRepository.UpdateReminderAsync(reminder);
                    }
                }

                _logger.LogInformation("Processed {Count} pending reminders", pendingReminders.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending reminders");
                throw;
            }
        }

        #region Helper Methods
        private string BuildConfirmationMessage(ReservationEntity reservation, string outletName)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {reservation.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Your reservation at {outletName} has been confirmed!");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Reservation Details:");
            messageBuilder.AppendLine($"- Code: {reservation.ReservationCode}");
            messageBuilder.AppendLine($"- Date: {reservation.ReservationDate.ToLocalTime():dddd, MMMM d, yyyy}");
            messageBuilder.AppendLine($"- Time: {reservation.ReservationDate.ToLocalTime():h:mm tt}");
            messageBuilder.AppendLine($"- Party Size: {reservation.PartySize}");

            if (!string.IsNullOrEmpty(reservation.SpecialRequests))
            {
                messageBuilder.AppendLine($"- Special Requests: {reservation.SpecialRequests}");
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Need to make changes? Reply to this message or call us.");

            return messageBuilder.ToString();
        }

        private string Build24HourReminderMessage(ReservationEntity reservation, string outletName)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {reservation.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"This is a friendly reminder about your reservation at {outletName} tomorrow:");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Reservation Details:");
            messageBuilder.AppendLine($"- Code: {reservation.ReservationCode}");
            messageBuilder.AppendLine($"- Date: {reservation.ReservationDate.ToLocalTime():dddd, MMMM d, yyyy}");
            messageBuilder.AppendLine($"- Time: {reservation.ReservationDate.ToLocalTime():h:mm tt}");
            messageBuilder.AppendLine($"- Party Size: {reservation.PartySize}");

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We look forward to welcoming you!");
            messageBuilder.AppendLine("If you need to cancel or modify your reservation, please do so at least 2 hours before your reserved time.");

            return messageBuilder.ToString();
        }

        private string Build1HourReminderMessage(ReservationEntity reservation, string outletName)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {reservation.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Your table at {outletName} will be ready soon!");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Reservation Details:");
            messageBuilder.AppendLine($"- Code: {reservation.ReservationCode}");
            messageBuilder.AppendLine($"- Time: {reservation.ReservationDate.ToLocalTime():h:mm tt}");
            messageBuilder.AppendLine($"- Party Size: {reservation.PartySize}");

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We're getting your table ready and look forward to serving you soon.");
            messageBuilder.AppendLine("Please arrive on time. If you're running late, please let us know.");

            return messageBuilder.ToString();
        }

        private string BuildGenericReminderMessage(ReservationEntity reservation, string outletName)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {reservation.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"This is a reminder about your upcoming reservation at {outletName}:");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Reservation Details:");
            messageBuilder.AppendLine($"- Code: {reservation.ReservationCode}");
            messageBuilder.AppendLine($"- Date: {reservation.ReservationDate.ToLocalTime():dddd, MMMM d, yyyy}");
            messageBuilder.AppendLine($"- Time: {reservation.ReservationDate.ToLocalTime():h:mm tt}");
            messageBuilder.AppendLine($"- Party Size: {reservation.PartySize}");

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We look forward to welcoming you!");

            return messageBuilder.ToString();
        }

        private string BuildCancellationMessage(ReservationEntity reservation, string outletName, string reason)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {reservation.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Your reservation at {outletName} has been cancelled.");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Cancelled Reservation Details:");
            messageBuilder.AppendLine($"- Code: {reservation.ReservationCode}");
            messageBuilder.AppendLine($"- Date: {reservation.ReservationDate.ToLocalTime():dddd, MMMM d, yyyy}");
            messageBuilder.AppendLine($"- Time: {reservation.ReservationDate.ToLocalTime():h:mm tt}");

            if (!string.IsNullOrEmpty(reason))
            {
                messageBuilder.AppendLine($"- Reason: {reason}");
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We hope to welcome you another time!");

            return messageBuilder.ToString();
        }

        private string BuildModificationMessage(ReservationEntity reservation, string outletName, string changes)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Hi {reservation.CustomerName},");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Your reservation at {outletName} has been updated!");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Updated Reservation Details:");
            messageBuilder.AppendLine($"- Code: {reservation.ReservationCode}");
            messageBuilder.AppendLine($"- Date: {reservation.ReservationDate.ToLocalTime():dddd, MMMM d, yyyy}");
            messageBuilder.AppendLine($"- Time: {reservation.ReservationDate.ToLocalTime():h:mm tt}");
            messageBuilder.AppendLine($"- Party Size: {reservation.PartySize}");

            if (!string.IsNullOrEmpty(reservation.SpecialRequests))
            {
                messageBuilder.AppendLine($"- Special Requests: {reservation.SpecialRequests}");
            }

            if (!string.IsNullOrEmpty(changes))
            {
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"Changes Made: {changes}");
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("We look forward to welcoming you!");

            return messageBuilder.ToString();
        }
        #endregion
    }

    // This interface would need to be implemented with an actual WhatsApp API integration
    public interface IWhatsAppService
    {
        Task SendMessageAsync(string phoneNumber, string message);
    }

    // Mock implementation for example purposes
    public class MockWhatsAppService : IWhatsAppService
    {
        private readonly ILogger<MockWhatsAppService> _logger;

        public MockWhatsAppService(ILogger<MockWhatsAppService> logger)
        {
            _logger = logger;
        }

        public Task SendMessageAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("MOCK: Sending WhatsApp message to {PhoneNumber}", phoneNumber);
            _logger.LogDebug("Message content: {Message}", message);

            // In a real implementation, this would call the WhatsApp API
            return Task.CompletedTask;
        }
    }
}