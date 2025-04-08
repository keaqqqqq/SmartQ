// FNBReservation.Modules.Reservation.API/Extensions/ReservationModuleExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Reservation.Infrastructure.Services;
using FNBReservation.Modules.Reservation.Infrastructure.Repositories;
using FNBReservation.Modules.Reservation.Infrastructure.Adapters;
using FNBReservation.Modules.Reservation.Infrastructure.Data;
using FNBReservation.Modules.Notification.Infrastructure.Extensions;

namespace FNBReservation.Modules.Reservation.API.Extensions
{
    public static class ReservationModuleExtensions
    {
        public static IServiceCollection AddReservationModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(ReservationModuleExtensions).Assembly);

            // Configure database context
            services.AddDbContext<ReservationDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
                )
            );

            services.AddNotificationModule(configuration);

            // Register repositories
            services.AddScoped<IReservationRepository, ReservationRepository>();

            // Register services
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IReservationNotificationService, ReservationNotificationService>();
            services.AddScoped<INearbyOutletsAvailabilityService, NearbyOutletsAvailabilityService>();

            // Register adapters
            services.AddScoped<IOutletAdapter, OutletAdapter>();
            services.AddScoped<ICustomerAdapter, CustomerAdapter>();

            // Register and configure WhatsApp service
            services.AddHttpClient();

            // Check if WhatsApp API is enabled in configuration
            var useRealWhatsAppApi = configuration.GetValue<bool>("WhatsAppApi:Enabled", false);

            // Register hosted service for table hold cleanup
            services.AddHostedService<TableHoldCleanupService>();

            // Register hosted service for processing reminders
            services.AddHostedService<ReminderProcessingService>();

            return services;
        }
    }
}