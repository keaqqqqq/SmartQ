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
using Microsoft.Extensions.Logging;
using FNBReservation.SharedKernel.Data;

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

            // Register DbContextFactory for read/write splitting
            services.AddScoped<DbContextFactory<QueueDbContext>>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<DbContextFactory<QueueDbContext>>();

                return new DbContextFactory<QueueDbContext>(
                    configuration,
                    (options, connectionString) => options.UseMySql(
                        connectionString,
                        ServerVersion.AutoDetect(connectionString)
                    ),
                    logger
                );
            });

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