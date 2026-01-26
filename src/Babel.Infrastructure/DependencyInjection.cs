using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Data;
using Babel.Infrastructure.Jobs;
using Babel.Infrastructure.Repositories;
using Babel.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace Babel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registrar opciones de configuración
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<SemanticKernelOptions>(configuration.GetSection(SemanticKernelOptions.SectionName));
        services.Configure<AzureOcrOptions>(configuration.GetSection(AzureOcrOptions.SectionName));
        services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));
        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));

        // SQL Server DbContext
        services.AddDbContext<BabelDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(BabelDbContext).Assembly.FullName)));

        // Register DbContext as IApplicationDbContext interface
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<BabelDbContext>());

        // Qdrant Client
        var qdrantEndpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
        Uri qdrantUri = new Uri(qdrantEndpoint);
        services.AddSingleton<QdrantClient>(sp =>
            new QdrantClient(qdrantUri));

        // Qdrant Initialization Service (crea la colección al inicio)
        services.AddHostedService<QdrantInitializationService>();

        // HttpClient for Azure OCR
        var azureOcrEndpoint = configuration["AzureComputerVision:Endpoint"] ?? "http://localhost:5000";
        services.AddHttpClient("AzureOcr", client =>
        {
            client.BaseAddress = new Uri(azureOcrEndpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Health Check Service
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // Storage Service
        services.AddScoped<IStorageService, LocalFileStorageService>();

        // File Type Detector
        services.AddSingleton<IFileTypeDetector, FileTypeDetectorService>();

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Text Extraction Service
        services.AddScoped<ITextExtractionService, TextExtractionService>();

        // Document Processing Job (registrado siempre, pero solo funciona si Hangfire está habilitado)
        services.AddScoped<DocumentProcessingJob>();

        // Configuration Validator
        services.AddSingleton<ConfigurationValidator>();

        // EF Core Health Check
        services.AddHealthChecks()
            .AddDbContextCheck<BabelDbContext>("database");

        return services;
    }

    /// <summary>
    /// Valida la configuración al inicio de la aplicación.
    /// </summary>
    public static bool ValidateConfiguration(this IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetRequiredService<ConfigurationValidator>();
        return validator.Validate();
    }
}
