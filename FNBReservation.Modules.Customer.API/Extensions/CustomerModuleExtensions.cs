using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Customer.Core.Interfaces;
using FNBReservation.Modules.Customer.Infrastructure.Data;
using FNBReservation.Modules.Customer.Infrastructure.Repositories;
using FNBReservation.Modules.Customer.Infrastructure.Services;
using FNBReservation.Modules.Customer.Infrastructure.Adapters;

namespace FNBReservation.Modules.Customer.API.Extensions
{
    public static class CustomerModuleExtensions
    {
        public static IServiceCollection AddCustomerModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(CustomerModuleExtensions).Assembly);

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