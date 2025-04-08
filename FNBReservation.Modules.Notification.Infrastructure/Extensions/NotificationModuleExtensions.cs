// NotificationModuleExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FNBReservation.Modules.Notification.Core.Interfaces;
using FNBReservation.Modules.Notification.Infrastructure.Services;

namespace FNBReservation.Modules.Notification.Infrastructure.Extensions
{
    public static class NotificationModuleExtensions
    {
        public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register HttpClient for WhatsApp API
            services.AddHttpClient();

            // Register notification services
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISmsService, SmsService>();

            // Register WhatsApp service with appropriate implementation based on configuration
            var useRealWhatsAppApi = configuration.GetValue<bool>("WhatsAppApi:Enabled", false);

            if (useRealWhatsAppApi)
            {
                // Use real WhatsApp API implementation
                services.AddScoped<IWhatsAppService, WhatsAppService>();
            }
            else
            {
                // Use mock implementation for development
                services.AddScoped<IWhatsAppService, MockWhatsAppService>();
            }

            return services;
        }
    }
}