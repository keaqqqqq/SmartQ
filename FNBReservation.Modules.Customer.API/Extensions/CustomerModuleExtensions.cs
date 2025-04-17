using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Customer.Core.Interfaces;
using FNBReservation.Modules.Customer.Infrastructure.Data;
using FNBReservation.Modules.Customer.Infrastructure.Repositories;
using FNBReservation.Modules.Customer.Infrastructure.Services;
using FNBReservation.Modules.Customer.Infrastructure.Adapters;
using Microsoft.Extensions.Logging;
using FNBReservation.SharedKernel.Data;

namespace FNBReservation.Modules.Customer.API.Extensions
{
    public static class CustomerModuleExtensions
    {
        public static IServiceCollection AddCustomerModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(CustomerModuleExtensions).Assembly);

            // Register DbContextFactory
            services.AddSingleton<DbContextFactory<CustomerDbContext>>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<DbContextFactory<CustomerDbContext>>>();

                return new DbContextFactory<CustomerDbContext>(
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

            // Configure database context
            services.AddDbContext<CustomerDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
                )
            );

            // Register repositories
            services.AddScoped<ICustomerRepository, CustomerRepository>();

            // Register services
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IReservationAdapter, ReservationAdapter>();
            services.AddScoped<ICustomerStatsService, CustomerStatsService>();
            return services;
        }
    }
}