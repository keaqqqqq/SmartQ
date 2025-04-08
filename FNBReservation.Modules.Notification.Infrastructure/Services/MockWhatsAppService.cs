// MockWhatsAppService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Notification.Core.Interfaces;

namespace FNBReservation.Modules.Notification.Infrastructure.Services
{
    public class MockWhatsAppService : IWhatsAppService
    {
        private readonly ILogger<MockWhatsAppService> _logger;

        public MockWhatsAppService(ILogger<MockWhatsAppService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendMessageAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("[MOCK] Would send WhatsApp message to {PhoneNumber}: {Message}",
                phoneNumber, message);

            return Task.CompletedTask;
        }

        public Task SendTemplateMessageAsync(string phoneNumber, string templateName, List<object> templateParams)
        {
            _logger.LogInformation("[MOCK] Would send WhatsApp template message to {PhoneNumber} using template {TemplateName}",
                phoneNumber, templateName);

            if (templateParams != null && templateParams.Count > 0)
            {
                _logger.LogInformation("[MOCK] Template parameters: {Parameters}",
                    string.Join(", ", templateParams));
            }

            return Task.CompletedTask;
        }
    }
}
