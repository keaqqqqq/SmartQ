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
using FNBReservation.Modules.Customer.API.Extensions;
using FNBReservation.Modules.Customer.Infrastructure.Data;
using FNBReservation.Modules.Queue.API.Extensions;
using FNBReservation.Modules.Queue.Infrastructure.Data;
using FNBReservation.Modules.Queue.Infrastructure.Hubs;
using FNBReservation.Modules.Notification.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks; 
using System.Text.Json;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure logging first
builder.Logging.ClearProviders(); 
builder.Logging.AddConsole(); 
builder.Logging.AddDebug(); 

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.Configure<ConsoleLoggerOptions>(options =>
{
    options.IncludeScopes = true; 
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; 
});

builder.Configuration.GetSection("Logging");

var startupLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");
startupLogger.LogInformation("======== APPLICATION STARTING ========");
startupLogger.LogDebug("Debug logging is enabled");
startupLogger.LogInformation("Instance ID: {InstanceId}",
    Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "local-dev");

builder.Services.AddControllers();

// Configure SignalR in more detail
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
})
.AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"), options =>
 {
     options.Configuration.ChannelPrefix = "fnbreservation";
 });

// Add distributed cache with Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "fnbreservation:";
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter("GlobalLimiter",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// Add HttpContextAccessor for accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

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
        OnMessageReceived = context =>
        {
            // Check if token exists in cookie
            string token = null;

            // First check for token in cookies if available
            if (context.Request.Cookies.ContainsKey("accessToken"))
            {
                token = context.Request.Cookies["accessToken"];
                // If token found in cookie, use it
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
            }

            // Support for WebSockets
            if (string.IsNullOrEmpty(context.Token) && context.Request.Path.StartsWithSegments("/queuehub"))
            {
                token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
            }

            return Task.CompletedTask;
        },
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

// Configure cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.SameAsRequest; // In production set to Always
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
            "http://host.docker.internal:5002",  // Frontend from Docker to API
            "http://localhost:5500"     // Websocket testing
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .SetIsOriginAllowed(origin => true); // For development - more restrictive in production
    });
});

// Add DB Contexts
builder.Services.AddDbContext<FNBDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    );
});

// Register modules
builder.Services.AddAuthenticationModule(builder.Configuration);
builder.Services.AddOutletModule(builder.Configuration);
builder.Services.AddReservationModule(builder.Configuration);
builder.Services.AddCustomerModule(builder.Configuration);
builder.Services.AddQueueModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks()
    // Use a lambda check instead of a custom class that might not exist
    .AddCheck("database", () =>
    {
        try
        {
            // Simple connection check using the DbContext factory
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FNBDbContext>();
            dbContext.Database.CanConnect();
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    },
    tags: new[] { "ready", "db" })
    .AddCheck("redis", () =>
     {
         try
         {
             var redisConnection = builder.Configuration.GetConnectionString("Redis");
             if (string.IsNullOrEmpty(redisConnection))
             {
                 return HealthCheckResult.Degraded("Redis connection string is not configured");
             }

             var redis = ConnectionMultiplexer.Connect(redisConnection);
             var db = redis.GetDatabase();
             // Simple ping test
             db.Ping();
             return HealthCheckResult.Healthy("Redis connection is healthy");
         }
         catch (Exception ex)
         {
             return HealthCheckResult.Unhealthy("Redis connection failed", ex);
         }
     }, tags: new[] { "ready", "cache" });

// Build the application
var app = builder.Build();

// Now configure health check endpoints AFTER app is built
// Define a custom response writer since HealthCheckUIResponseWriter is not available
static Task WriteHealthCheckResponse(HttpContext context, HealthReport result)
{
    context.Response.ContentType = "application/json";

    var options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    var responseJson = JsonSerializer.Serialize(new
    {
        status = result.Status.ToString(),
        results = result.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            data = e.Value.Data
        })
    }, options);

    return context.Response.WriteAsync(responseJson);
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
});

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

    // Log authentication info
    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        var headerValue = authHeader.ToString();
        var truncatedValue = headerValue.Length > 20
            ? headerValue.Substring(0, 20) + "..."
            : headerValue;

        logger.LogInformation("Authorization header present: {AuthHeader}", truncatedValue);
    }
    else if (context.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
    {
        var truncatedValue = cookieToken.Length > 20
            ? cookieToken.Substring(0, 20) + "..."
            : cookieToken;

        logger.LogInformation("Access token cookie present: {CookieToken}", truncatedValue);
    }
    else
    {
        logger.LogWarning("No authorization token found in request");
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
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseCookiePolicy(); // Add cookie policy middleware
app.UseRouting();              // Add this if missing
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<QueueHub>("/queuehub");

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Apply Authentication DB migrations
        var authContext = services.GetRequiredService<FNBDbContext>();
        authContext.Database.Migrate();

        // Apply Outlet DB migrations
        var outletContext = services.GetRequiredService<OutletDbContext>();
        outletContext.Database.Migrate();

        // Apply Reservation DB migrations
        var reservationContext = services.GetRequiredService<ReservationDbContext>();
        reservationContext.Database.Migrate();

        // Apply Customer DB migrations
        var customerContext = services.GetRequiredService<CustomerDbContext>();
        customerContext.Database.Migrate();

        // Apply Queue DB migrations
        var queueContext = services.GetRequiredService<QueueDbContext>();
        queueContext.Database.Migrate();

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