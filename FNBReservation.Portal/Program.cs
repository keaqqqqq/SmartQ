using MudBlazor.Services;
using FNBReservation.Portal.Components;
using FNBReservation.Portal.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => 
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddCascadingAuthenticationState();

// Register HttpClient for authentication API
builder.Services.AddHttpClient();

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add CORS policy for APIs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApiServer", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000",
                "https://localhost:5001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Important for cookies
    });
});

// Add cookie policy for authentication
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Allow cookies to be sent with cross-site requests (important for APIs)
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

// Register authentication services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Register the API Authorization Handler
builder.Services.AddTransient<ApiAuthorizationHandler>();

// Configure HttpClient with Authorization Handler
builder.Services.AddHttpClient("API", (sp, client) =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    
    // Configure to send cookies with requests
    client.DefaultRequestHeaders.Add("X-Include-Credentials", "true");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = true,
    CookieContainer = new CookieContainer(),
    // Allow redirects
    AllowAutoRedirect = true,
    // Use default credentials
    UseDefaultCredentials = false,
    // Set credentials to pass cookies
    Credentials = CredentialCache.DefaultCredentials
})
.AddHttpMessageHandler<ApiAuthorizationHandler>();

// Register AuthService with the API HttpClient
builder.Services.AddScoped<AuthService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var tokenService = sp.GetRequiredService<JwtTokenService>();
    
    return new AuthService(
        httpClientFactory.CreateClient("API"),
        jsRuntime,
        authStateProvider,
        configuration,
        httpContextAccessor,
        tokenService
    );
});

// Configure Authentication and Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "FNBReservation.Auth";
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = context =>
    {
        // For API requests return 401
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        
        // For browser requests redirect to login page
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// Register the real HttpClientPeakHourService for peak hour management
builder.Services.AddScoped<IPeakHourService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jwtTokenService = sp.GetRequiredService<JwtTokenService>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    return new HttpClientPeakHourService(
        httpClientFactory.CreateClient("API"),
        jwtTokenService,
        jsRuntime,
        configuration
    );
});

// Register the real HttpClientOutletService for outlet management
builder.Services.AddScoped<IOutletService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jwtTokenService = sp.GetRequiredService<JwtTokenService>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var peakHourService = sp.GetRequiredService<IPeakHourService>();
    
    return new HttpClientOutletService(
        httpClientFactory.CreateClient("API"),
        jwtTokenService,
        jsRuntime,
        configuration,
        peakHourService
    );
});

// Register TableService
builder.Services.AddScoped<ITableService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var jwtTokenService = sp.GetRequiredService<JwtTokenService>();
    var logger = sp.GetRequiredService<ILogger<TableService>>();
    
    // Use the API client that has the auth handler configured
    return new TableService(httpClientFactory.CreateClient("API"), configuration, jwtTokenService, logger);
});

// Register other mock services
builder.Services.AddScoped<IStaffService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jwtTokenService = sp.GetRequiredService<JwtTokenService>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<HttpClientStaffService>>();
    
    // Use the API client that has the auth handler configured
    return new HttpClientStaffService(
        httpClientFactory.CreateClient("API"),
        jwtTokenService,
        jsRuntime,
        configuration,
        logger
    );
});
builder.Services.AddScoped<ICustomerService, MockCustomerService>();
builder.Services.AddScoped<IReservationService, MockReservationService>();
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // In development, show detailed errors
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Enable CORS
app.UseCors("AllowApiServer");
app.UseCookiePolicy();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();