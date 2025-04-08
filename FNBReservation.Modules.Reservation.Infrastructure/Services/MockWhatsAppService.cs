// In FNBReservation.Modules.Reservation.Infrastructure/Services/MockWhatsAppService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Notification.Core.Interfaces;

namespace FNBReservation.Modules.Reservation.Infrastructure.Services
{
    // Mock implementation for example purposes and development/testing
    public class MockWhatsAppService : IWhatsAppService
    {
        private readonly ILogger<MockWhatsAppService> _logger;

        public MockWhatsAppService(ILogger<MockWhatsAppService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendMessageAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("MOCK: Sending WhatsApp message to {PhoneNumber}", phoneNumber);
            _logger.LogDebug("Message content: {Message}", message);

            // In a real implementation, this would call the WhatsApp API
            return Task.CompletedTask;
        }

        public Task SendTemplateMessageAsync(string phoneNumber, string templateName, List<object> templateParams)
        {
            _logger.LogInformation("MOCK: Sending WhatsApp template message to {PhoneNumber}", phoneNumber);
            _logger.LogDebug("Template name: {TemplateName}", templateName);

            if (templateParams != null && templateParams.Count > 0)
            {
                _logger.LogDebug("Template parameters: {Parameters}", string.Join(", ", templateParams));
            }

            // In a real implementation, this would call the WhatsApp API with a template
            return Task.CompletedTask;
        }
    }
}