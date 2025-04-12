using MudBlazor.Services;
using FNBReservation.Portal.Components;
using FNBReservation.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveServerComponents();


// Register the mock services
builder.Services.AddSingleton<IOutletService, MockOutletService>();
builder.Services.AddScoped<IStaffService, MockStaffService>();
builder.Services.AddScoped<ICustomerService, MockCustomerService>();
builder.Services.AddScoped<IReservationService, MockReservationService>(); // Add this line
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();