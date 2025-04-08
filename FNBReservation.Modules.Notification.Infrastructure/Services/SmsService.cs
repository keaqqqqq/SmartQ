// SmsService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Notification.Core.Interfaces;

namespace FNBReservation.Modules.Notification.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly IConfiguration _configuration;

        public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task SendMessageAsync(string phoneNumber, string message)
        {
            // This is a placeholder. In a real implementation, you would integrate with an SMS gateway
            _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}", phoneNumber, message);

            // For now, we'll mock the implementation
            _logger.LogInformation("[MOCK] SMS would be sent to {PhoneNumber}", phoneNumber);

            return Task.CompletedTask;
        }
    }
}