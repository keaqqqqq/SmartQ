// FNBReservation.Modules.Queue.API/Extensions/QueueModuleExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Queue.Infrastructure.Data;
using FNBReservation.Modules.Queue.Infrastructure.Repositories;
using FNBReservation.Modules.Queue.Infrastructure.Services;
using FNBReservation.Modules.Notification.Infrastructure.Extensions;

namespace FNBReservation.Modules.Queue.API.Extensions
{
    public static class QueueModuleExtensions
    {
        public static IServiceCollection AddQueueModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(QueueModuleExtensions).Assembly);

            // Configure database context
            services.AddDbContext<QueueDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
                )
            );

            services.AddNotificationModule(configuration);

            // Register SignalR hub
            services.AddSingleton<IQueueHub, QueueHubService>();

            // Register repositories
            services.AddScoped<IQueueRepository, QueueRepository>();

            // Register services
            services.AddScoped<IQueueService, QueueService>();
            services.AddScoped<IQueueNotificationService, QueueNotificationService>();
            services.AddScoped<IWaitTimeEstimationService, WaitTimeEstimationService>();

            services.AddScoped<IQueueMaintenanceService, QueueMaintenanceService>();
            services.AddHostedService<QueueCleanupBackgroundService>();

            return services;
        }
    }
}