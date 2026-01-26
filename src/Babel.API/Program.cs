using Babel.Application;
using Babel.Infrastructure;
using Babel.Infrastructure.Configuration;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Load local settings with secrets (not committed to git)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Babel API",
        Version = "v1",
        Description = "API para el sistema de gestión documental Babel con capacidades de IA y OCR"
    });
});

// Add Application layer (MediatR, Validators, Behaviors)
builder.Services.AddApplication();

// Add Infrastructure layer (includes DbContext, Qdrant, Azure OCR, Health Checks)
builder.Services.AddInfrastructure(builder.Configuration);

// Hangfire Configuration (solo si hay conexión disponible)
var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection");
if (string.IsNullOrWhiteSpace(hangfireConnection))
    hangfireConnection = builder.Configuration.GetConnectionString("DefaultConnection");

var hangfireOptions = builder.Configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>() ?? new HangfireOptions();
var hangfireEnabled = !string.IsNullOrWhiteSpace(hangfireConnection);

if (hangfireEnabled)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            SchemaName = "HangFire"
        }));

    // Add Hangfire Server
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = hangfireOptions.WorkerCount;
        options.Queues = new[] { "default", "documents" };
    });

    // Registrar servicios de procesamiento de documentos que dependen de Hangfire
    builder.Services.AddScoped<Babel.Application.Interfaces.IBackgroundJobService,
        Babel.Infrastructure.Services.HangfireBackgroundJobService>();
    builder.Services.AddScoped<Babel.Application.Interfaces.IDocumentProcessingQueue,
        Babel.Infrastructure.Services.DocumentProcessingQueue>();
}
else
{
    Console.WriteLine("WARNING: Hangfire deshabilitado - no hay cadena de conexión configurada");
}

var app = builder.Build();

// Validate configuration on startup
if (!app.Services.ValidateConfiguration())
{
    app.Logger.LogWarning("La aplicación continuará con advertencias de configuración");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Babel API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map health checks endpoint
app.MapHealthChecks("/health");

// Map Hangfire Dashboard (solo si está habilitado)
if (hangfireEnabled)
{
    app.MapHangfireDashboard(hangfireOptions.DashboardPath, new DashboardOptions
    {
        DashboardTitle = "Babel - Jobs Dashboard",
        // En desarrollo permitir acceso sin autenticación
        // En producción agregar autenticación
        Authorization = app.Environment.IsDevelopment()
            ? Array.Empty<IDashboardAuthorizationFilter>()
            : new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
    });
}

app.Run();
