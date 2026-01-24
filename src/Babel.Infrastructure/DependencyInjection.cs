using Babel.Application.Interfaces;
using Babel.Infrastructure.Data;
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

        // HttpClient for Azure OCR
        var azureOcrEndpoint = configuration["AzureComputerVision:Endpoint"] ?? "http://localhost:5000";
        services.AddHttpClient("AzureOcr", client =>
        {
            client.BaseAddress = new Uri(azureOcrEndpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Health Check Service
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // EF Core Health Check
        services.AddHealthChecks()
            .AddDbContextCheck<BabelDbContext>("database");

        return services;
    }
}
