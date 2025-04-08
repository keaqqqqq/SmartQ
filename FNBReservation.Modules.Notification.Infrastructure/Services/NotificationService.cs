// NotificationService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Notification.Core.Interfaces;

namespace FNBReservation.Modules.Notification.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly ISmsService _smsService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IWhatsAppService whatsAppService,
            ISmsService smsService,
            ILogger<NotificationService> logger)
        {
            _whatsAppService = whatsAppService ?? throw new ArgumentNullException(nameof(whatsAppService));
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendNotificationAsync(string phoneNumber, string message, string preferredChannel = "WhatsApp")
        {
            try
            {
                _logger.LogInformation("Sending notification via {Channel} to {PhoneNumber}",
                    preferredChannel, phoneNumber);

                switch (preferredChannel.ToLowerInvariant())
                {
                    case "whatsapp":
                        await _whatsAppService.SendMessageAsync(phoneNumber, message);
                        break;

                    case "sms":
                        await _smsService.SendMessageAsync(phoneNumber, message);
                        break;

                    default:
                        _logger.LogWarning("Unknown notification channel: {Channel}. Defaulting to WhatsApp.",
                            preferredChannel);
                        await _whatsAppService.SendMessageAsync(phoneNumber, message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to {PhoneNumber}", phoneNumber);
                throw;
            }
        }
    }
}