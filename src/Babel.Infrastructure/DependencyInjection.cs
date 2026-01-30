using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Data;
using Babel.Infrastructure.Jobs;
using Babel.Infrastructure.Queues;
using Babel.Infrastructure.Repositories;
using Babel.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
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

        // Document Processing Jobs
        services.AddScoped<DocumentProcessingJob>();

        // Vectorization Services
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
        services.AddScoped<IVectorStoreService, QdrantVectorStoreService>();
        services.AddScoped<DocumentVectorizationJob>();

        // Chat RAG Service
        services.AddScoped<IChatService, SemanticKernelChatService>();

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
                    var ollamaEndpoint = new Uri(skOptions.Ollama.Endpoint);
                    kernelBuilder.AddOllamaTextEmbeddingGeneration(
                        modelId: skOptions.Ollama.EmbeddingModel,
                        endpoint: ollamaEndpoint);
                    kernelBuilder.AddOllamaChatCompletion(
                        modelId: skOptions.Ollama.ChatModel,
                        endpoint: ollamaEndpoint);
                }
                break;

            case "openai":
                if (!string.IsNullOrEmpty(skOptions.OpenAI?.ApiKey))
                {
                    kernelBuilder.AddOpenAITextEmbeddingGeneration(
                        modelId: skOptions.OpenAI.EmbeddingModel,
                        apiKey: skOptions.OpenAI.ApiKey);
                    kernelBuilder.AddOpenAIChatCompletion(
                        modelId: skOptions.OpenAI.ChatModel,
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

    /// <summary>
    /// Agrega los servicios de Hangfire para procesamiento en segundo plano.
    /// Debe llamarse DESPUÉS de AddInfrastructure.
    /// </summary>
    /// <param name="services">Colección de servicios</param>
    /// <param name="configuration">Configuración de la aplicación</param>
    /// <returns>True si Hangfire fue habilitado, false si no hay connection string</returns>
    public static bool AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var hangfireConnection = configuration.GetConnectionString("HangfireConnection");
        if (string.IsNullOrWhiteSpace(hangfireConnection))
            hangfireConnection = configuration.GetConnectionString("DefaultConnection");

        var hangfireEnabled = !string.IsNullOrWhiteSpace(hangfireConnection);

        if (hangfireEnabled)
        {
            var hangfireOptions = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
                ?? new HangfireOptions();

            services.AddHangfire(config => config
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
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = hangfireOptions.WorkerCount;
                options.Queues = new[] { "default", "documents" };
            });

            // Registrar servicios de procesamiento que dependen de Hangfire
            services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
            services.AddScoped<IDocumentProcessingQueue, DocumentProcessingQueue>();
        }
        else
        {
            // Fallback: registrar implementación no-op que advierte al usuario
            services.AddScoped<IDocumentProcessingQueue, InMemoryDocumentProcessingQueue>();
            Console.WriteLine("WARNING: Hangfire deshabilitado - no hay cadena de conexión configurada. " +
                "Los documentos NO serán procesados automáticamente.");
        }

        return hangfireEnabled;
    }
}
