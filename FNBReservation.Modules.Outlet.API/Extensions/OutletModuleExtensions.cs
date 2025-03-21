// FNBReservation.Modules.Outlet.API/Extensions/OutletModuleExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using FNBReservation.Modules.Outlet.Infrastructure.Repositories;
using FNBReservation.Modules.Outlet.Infrastructure.Services;

namespace FNBReservation.Modules.Outlet.API.Extensions
{
    public static class OutletModuleExtensions
    {
        public static IServiceCollection AddOutletModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(OutletModuleExtensions).Assembly);

            // Configure database context
            services.AddDbContext<OutletDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
                )
            );

            // Register module-specific services
            services.AddScoped<IOutletRepository, OutletRepository>();
            services.AddScoped<ITableRepository, TableRepository>();
            services.AddScoped<IPeakHourRepository, PeakHourRepository>();


            services.AddScoped<IOutletService, OutletService>();
            services.AddScoped<IGeolocationService, GeolocationService>();
            services.AddScoped<ITableService, TableService>();
            services.AddScoped<IPeakHourService, PeakHourService>();



            return services;
        }
    }
}