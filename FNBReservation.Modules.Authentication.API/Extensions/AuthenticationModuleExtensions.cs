// Path: FNBReservation.Modules.Authentication.API/Extensions/AuthenticationModuleExtensions.cs
using FNBReservation.Modules.Authentication.Infrastructure.Services;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Infrastructure.Services.Notification;
using FNBReservation.Modules.Authentication.Infrastructure.Adapters;

namespace FNBReservation.Modules.Authentication.API.Extensions
{
    public static class AuthenticationModuleExtensions
    {
        public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(AuthenticationModuleExtensions).Assembly);

            // Configure and validate SMTP settings
            var smtpSection = configuration.GetSection("SmtpSettings");
            if (string.IsNullOrEmpty(smtpSection["Host"]))
            {
                // Log warning that SMTP is not properly configured
                Console.WriteLine("WARNING: SMTP settings are not properly configured. Email functionality will not work correctly.");
            }

            // Register module-specific services
            // Uncomment these when your services are properly migrated to the modular structure
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>(); // Add this line
            services.AddScoped<IStaffService, StaffService>(); // Add this line
            services.AddScoped<IOutletAdapter, OutletAdapter>(); // Add this line

            return services;
        }
    }
}