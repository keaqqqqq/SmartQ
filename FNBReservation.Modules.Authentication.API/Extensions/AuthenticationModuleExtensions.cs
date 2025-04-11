// Path: FNBReservation.Modules.Authentication.API/Extensions/AuthenticationModuleExtensions.cs
using FNBReservation.Modules.Authentication.Infrastructure.Services;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Infrastructure.Services.Notification;
using FNBReservation.Modules.Authentication.Infrastructure.Adapters;
using Microsoft.AspNetCore.Http;

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

            // Ensure HttpContextAccessor is registered
            services.AddHttpContextAccessor();

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