using Babel.Application;
using Babel.Infrastructure;
using Babel.Infrastructure.Configuration;
using Babel.WebUI.Services;
using Hangfire;
using Hangfire.Dashboard;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Load local settings with secrets (not committed to git)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Theme service
builder.Services.AddScoped<IThemeService, ThemeService>();

// Add HttpClient for Chat API
builder.Services.AddHttpClient<IChatApiService, ChatApiService>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001/";
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // Timeout largo para streaming
});

// Add Razor Pages and Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Add Application layer (MediatR, Validators, Behaviors)
builder.Services.AddApplication();

// Add Infrastructure services (includes IHealthCheckService)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Hangfire services for background job processing
var hangfireEnabled = builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

// Validate configuration on startup
if (!app.Services.ValidateConfiguration())
{
    app.Logger.LogWarning("La aplicación continuará con advertencias de configuración");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Map Hangfire Dashboard (solo si está habilitado)
if (hangfireEnabled)
{
    var hangfireOptions = builder.Configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
        ?? new HangfireOptions();

    app.MapHangfireDashboard(hangfireOptions.DashboardPath, new DashboardOptions
    {
        DashboardTitle = "Babel - Jobs Dashboard",
        // En desarrollo permitir acceso sin autenticación
        Authorization = app.Environment.IsDevelopment()
            ? Array.Empty<IDashboardAuthorizationFilter>()
            : new[] { new LocalRequestsOnlyAuthorizationFilter() }
    });
}

app.Run();
