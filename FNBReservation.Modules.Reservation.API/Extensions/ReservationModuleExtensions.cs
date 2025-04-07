using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Reservation.Infrastructure.Services;
using FNBReservation.Modules.Reservation.Infrastructure.Repositories;
using FNBReservation.Modules.Reservation.Infrastructure.Adapters;
using FNBReservation.Modules.Reservation.Infrastructure.Data;

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

            // Register repositories
            services.AddScoped<IReservationRepository, ReservationRepository>();

            // Register services
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IReservationNotificationService, ReservationNotificationService>();
            services.AddScoped<IOutletAdapter, OutletAdapter>();

            // Register the WhatsApp service (mock for development)
            services.AddScoped<IWhatsAppService, MockWhatsAppService>();

            // Register hosted service for processing reminders (would be uncommented in the real implementation)
            // services.AddHostedService<ReminderProcessingService>();

            return services;
        }
    }
}