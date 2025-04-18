// Path: FNBReservation.Modules.Authentication.API/Extensions/AuthenticationModuleExtensions.cs
using FNBReservation.Modules.Authentication.Infrastructure.Services;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Infrastructure.Services.Notification;
using FNBReservation.Modules.Authentication.Infrastructure.Adapters;
using FNBReservation.SharedKernel.Data;
using FNBReservation.Modules.Authentication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FNBReservation.Modules.Authentication.Infrastructure.Repositories;

namespace FNBReservation.Modules.Authentication.API.Extensions
{
    public static class AuthenticationModuleExtensions
    {
        public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(AuthenticationModuleExtensions).Assembly);

            // Register DbContextFactory for read/write splitting
            services.AddScoped<DbContextFactory<FNBDbContext>>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<DbContextFactory<FNBDbContext>>();

                return new DbContextFactory<FNBDbContext>(
                    configuration,
                    (options, connectionString) => options.UseMySql(
                        connectionString,
                        ServerVersion.AutoDetect(connectionString)
                    ),
                    logger
                );
            });

            // Configure and validate SMTP settings
            var smtpSection = configuration.GetSection("SmtpSettings");
            if (string.IsNullOrEmpty(smtpSection["Host"]))
            {
                // Log warning that SMTP is not properly configured
                Console.WriteLine("WARNING: SMTP settings are not properly configured. Email functionality will not work correctly.");
            }

            // Ensure HttpContextAccessor is registered
            services.AddHttpContextAccessor();

            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IStaffRepository, StaffRepository>();


            // Register module-specific services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IStaffService, StaffService>();
            services.AddScoped<IOutletAdapter, OutletAdapter>();

            return services;
        }
    }
}