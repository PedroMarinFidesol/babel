using Babel.Infrastructure;

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

// Add Infrastructure layer (includes DbContext, Qdrant, Azure OCR, Health Checks)
builder.Services.AddInfrastructure(builder.Configuration);

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

app.Run();
