// FNBReservation.MainAPI/Program.cs
using FNBReservation.Modules.Authentication.Infrastructure.Data;
using FNBReservation.Modules.Authentication.API.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging.Console;
using FNBReservation.Modules.Outlet.API.Extensions;
using FNBReservation.Modules.Outlet.Infrastructure.Data;
using FNBReservation.Modules.Reservation.API.Extensions;
using FNBReservation.Modules.Reservation.Infrastructure.Data;

// Other module extensions

var builder = WebApplication.CreateBuilder(args);

// Configure logging first
builder.Logging.ClearProviders(); // Clear default providers
builder.Logging.AddConsole(); // Add console logger explicitly
builder.Logging.AddDebug(); // Also log to debug output window

// Set minimum log level to Information or Debug for more detailed logs
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add custom console logger configuration
builder.Services.Configure<ConsoleLoggerOptions>(options =>
{
    options.IncludeScopes = true; // Include log scopes
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; // Add timestamps to logs
});

// Configure appsettings.json logging (make sure this section exists in your appsettings.json)
// This can be used to override log levels for specific categories
builder.Configuration.GetSection("Logging");

// Create a simple test log to verify configuration is working
var startupLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");
startupLogger.LogInformation("======== APPLICATION STARTING ========");
startupLogger.LogDebug("Debug logging is enabled");

builder.Services.AddControllers();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FNB Reservation API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
                logger.LogWarning("Token expired: {ExceptionMessage}", context.Exception.Message);
            }
            else
            {
                logger.LogError(context.Exception, "Authentication failed");
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated successfully for user: {NameIdentifier}",
                context.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication challenge issued: {Error}, {ErrorDescription}",
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// Add Authorization - KEEP THIS
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "OutletStaff"));
});

// Add CORS - KEEP THIS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5002",     // Frontend in local dev (non-Docker)
            "https://localhost:5003",    // Frontend in local dev HTTPS (non-Docker)
            "http://localhost:5000",     // API in Docker (if testing from the same host)
            "http://host.docker.internal:5002"  // Frontend from Docker to API
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddDbContext<FNBDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Register modules
builder.Services.AddAuthenticationModule(builder.Configuration);
builder.Services.AddOutletModule(builder.Configuration);
builder.Services.AddReservationModule(builder.Configuration); // Add this line

// Other modules...

var app = builder.Build();

// Early log to confirm application built successfully
app.Logger.LogInformation("Application built successfully");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("Running in Development environment");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.Logger.LogInformation("Running in {Environment} environment", app.Environment.EnvironmentName);
}

// Add debug middleware for request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogInformation(
        "Request {Method} {Path}{QueryString}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "");

    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        var headerValue = authHeader.ToString();
        var truncatedValue = headerValue.Length > 20
            ? headerValue.Substring(0, 20) + "..."
            : headerValue;

        logger.LogInformation("Authorization header present: {AuthHeader}", truncatedValue);
    }
    else
    {
        logger.LogWarning("No Authorization header found in request");
    }

    // Capture the original body stream
    var originalBodyStream = context.Response.Body;

    try
    {
        // Log response info after request completes
        await next();

        logger.LogInformation(
            "Response completed with status code: {StatusCode}",
            context.Response.StatusCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Request processing failed");
        throw;
    }
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var outletContext = services.GetRequiredService<OutletDbContext>();
        outletContext.Database.Migrate();

        // Add this section:
        var reservationContext = services.GetRequiredService<ReservationDbContext>();
        reservationContext.Database.Migrate();

        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying migrations: {Message}", ex.Message);
    }
}

app.Logger.LogInformation("======== APPLICATION STARTUP COMPLETE ========");

try
{
    app.Run();
    app.Logger.LogInformation("Application shutdown complete");
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Application terminated unexpectedly");
}