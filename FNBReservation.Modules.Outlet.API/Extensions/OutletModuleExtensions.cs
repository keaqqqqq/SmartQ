// FNBReservation.Modules.Outlet.API/Extensions/OutletModuleExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using FNBReservation.Modules.Outlet.Infrastructure.Repositories;
using FNBReservation.Modules.Outlet.Infrastructure.Services;
using FNBReservation.SharedKernel.Data;
using System;

namespace FNBReservation.Modules.Outlet.API.Extensions
{
    public static class OutletModuleExtensions
    {
        public static IServiceCollection AddOutletModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(OutletModuleExtensions).Assembly);

            // Register DbContextFactory
            services.AddSingleton<DbContextFactory<OutletDbContext>>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<DbContextFactory<OutletDbContext>>>();

                return new DbContextFactory<OutletDbContext>(
                    config,
                    (builder, connectionString) =>
                    {
                        builder.UseMySql(
                            connectionString,
                            ServerVersion.AutoDetect(connectionString),
                            options =>
                            {
                                options.EnableRetryOnFailure(
                                    maxRetryCount: 5,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorNumbersToAdd: null);

                                // Set command timeout from configuration
                                var timeout = config.GetValue<int>("DatabaseOptions:CommandTimeout", 30);
                                options.CommandTimeout(timeout);
                            }
                        );
                    },
                    logger);
            });

            // Keep the regular DbContext registration for services that require direct access
            services.AddDbContext<OutletDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
                )
            );

            // Register repositories and services
            services.AddScoped<IOutletRepository, OutletRepository>();
            services.AddScoped<ITableRepository, TableRepository>();
            services.AddScoped<IPeakHourRepository, PeakHourRepository>();

            services.AddScoped<IOutletService, OutletService>();
            services.AddScoped<IGeolocationService, GeolocationService>();
            services.AddScoped<ITableService, TableService>();
            services.AddScoped<IPeakHourService, PeakHourService>();
            services.AddScoped<ITableTypeService, TableTypeService>();

            return services;
        }
    }
}