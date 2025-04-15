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

// Add cookie policy for authentication
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// Register authentication services
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Register CustomAuthStateProvider with proper dependencies
builder.Services.AddScoped<CustomAuthStateProvider>(sp => {
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var logger = sp.GetRequiredService<ILogger<CustomAuthStateProvider>>();
    var tokenService = sp.GetRequiredService<JwtTokenService>();
    
    return new CustomAuthStateProvider(httpClient, jsRuntime, logger, tokenService);
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

// Configure the API HttpClient
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<ApiAuthorizationHandler>();

// Register JwtTokenService
builder.Services.AddScoped<JwtTokenService>();

// Register ApiAuthorizationHandler - it depends on AuthService, but AuthService depends on it too
// To break this circular dependency, we'll register AuthService first with a basic HttpClient
builder.Services.AddScoped<AuthService>(sp => {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/");
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var tokenService = sp.GetRequiredService<JwtTokenService>();
    
    return new AuthService(
        httpClient,
        jsRuntime,
        authStateProvider,
        configuration,
        httpContextAccessor,
        tokenService
    );
});

// Register the API Authorization Handler
builder.Services.AddTransient<ApiAuthorizationHandler>();

// Register the real HttpClientPeakHourService for peak hour management
builder.Services.AddScoped<IPeakHourService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    return new HttpClientPeakHourService(
        httpClientFactory.CreateClient("API"),
        jsRuntime,
        configuration
    );
});

// Register the real HttpClientOutletService for outlet management
builder.Services.AddScoped<IOutletService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var peakHourService = sp.GetRequiredService<IPeakHourService>();
    
    return new HttpClientOutletService(
        httpClientFactory.CreateClient("API"),
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
    var logger = sp.GetRequiredService<ILogger<TableService>>();
    
    // Use the API client that has the auth handler configured
    return new TableService(
        httpClientFactory.CreateClient("API"), 
        configuration, 
        logger
    );
});

// Register other services
builder.Services.AddScoped<IStaffService>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<HttpClientStaffService>>();
    
    // Use the API client that has the auth handler configured
    return new HttpClientStaffService(
        httpClientFactory.CreateClient("API"),
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
app.UseCookiePolicy();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();