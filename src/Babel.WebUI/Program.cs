using Babel.Application;
using Babel.Infrastructure;
using Babel.WebUI.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Load local settings with secrets (not committed to git)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Razor Pages and Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Application layer (MediatR, Validators, Behaviors)
builder.Services.AddApplication();

// Add Infrastructure services (includes IHealthCheckService)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Mock Data Service (temporary until MediatR is implemented)
builder.Services.AddScoped<MockDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
