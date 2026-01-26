using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Data;
using Babel.Infrastructure.Jobs;
using Babel.Infrastructure.Queues;
using Babel.Infrastructure.Repositories;
using Babel.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;

#pragma warning disable SKEXP0070 // Ollama connector is experimental

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

        // Qdrant Client - usa gRPC (puerto 6334 por defecto)
        var qdrantHost = configuration["Qdrant:Host"] ?? "localhost";
        var qdrantGrpcPort = configuration.GetValue("Qdrant:GrpcPort", 6334);
        services.AddSingleton<QdrantClient>(sp =>
            new QdrantClient(qdrantHost, qdrantGrpcPort));

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
        // Asegurar que exista una implementación de IDocumentProcessingQueue.
        // Si existe una implementación específica (p. ej. HangfireDocumentProcessingQueue) se debe registrar
        // en función de la configuración. Aquí registramos una implementación en memoria por defecto
        // para evitar fallos de validación del contenedor DI cuando la cola no esté configurada.
        services.AddScoped<IDocumentProcessingQueue, InMemoryDocumentProcessingQueue>();

        services.AddScoped<DocumentProcessingJob>();

        // Vectorization Services
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
        services.AddScoped<IVectorStoreService, QdrantVectorStoreService>();
        services.AddScoped<DocumentVectorizationJob>();

        // Semantic Kernel Embedding Generator
        AddSemanticKernelEmbeddingGenerator(services, configuration);

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

    /// <summary>
    /// Configura el generador de embeddings según el proveedor seleccionado.
    /// </summary>
    private static void AddSemanticKernelEmbeddingGenerator(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var skOptions = configuration.GetSection(SemanticKernelOptions.SectionName).Get<SemanticKernelOptions>();

        if (skOptions is null)
        {
            return;
        }

        var kernelBuilder = Kernel.CreateBuilder();

        switch (skOptions.DefaultProvider?.ToLowerInvariant())
        {
            case "ollama":
                if (!string.IsNullOrEmpty(skOptions.Ollama?.Endpoint))
                {
                    kernelBuilder.AddOllamaTextEmbeddingGeneration(
                        modelId: skOptions.Ollama.EmbeddingModel,
                        endpoint: new Uri(skOptions.Ollama.Endpoint));
                }
                break;

            case "openai":
                if (!string.IsNullOrEmpty(skOptions.OpenAI?.ApiKey))
                {
                    kernelBuilder.AddOpenAITextEmbeddingGeneration(
                        modelId: skOptions.OpenAI.EmbeddingModel,
                        apiKey: skOptions.OpenAI.ApiKey);
                }
                break;
        }

        var kernel = kernelBuilder.Build();

        // Registrar el Kernel para inyección y reutilización
        // Evitamos resolver aquí un IEmbeddingGenerator concreto porque los conectores
        // pueden no registrar exactamente ese contrato en el ServiceProvider interno del Kernel.

        services.AddSingleton(kernel);
    }
}
