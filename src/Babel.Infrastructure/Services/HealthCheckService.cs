using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using System.Diagnostics;

namespace Babel.Infrastructure.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly BabelDbContext _dbContext;
    private readonly QdrantClient _qdrantClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        BabelDbContext dbContext,
        QdrantClient qdrantClient,
        IHttpClientFactory httpClientFactory,
        ILogger<HealthCheckService> logger)
    {
        _dbContext = dbContext;
        _qdrantClient = qdrantClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckSqlServerAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simple query to test connection
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("SQL Server health check passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return new HealthCheckResult
            {
                ServiceName = "SQL Server",
                IsHealthy = true,
                Message = "Successfully connected to SQL Server",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "SQL Server health check failed");

            return new HealthCheckResult
            {
                ServiceName = "SQL Server",
                IsHealthy = false,
                Message = $"Failed to connect: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<HealthCheckResult> CheckQdrantAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // List collections to verify Qdrant is accessible
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Qdrant health check passed in {ElapsedMs}ms. Found {CollectionCount} collections",
                stopwatch.ElapsedMilliseconds, collections.Count);

            return new HealthCheckResult
            {
                ServiceName = "Qdrant",
                IsHealthy = true,
                Message = $"Successfully connected to Qdrant. Found {collections.Count} collections",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Qdrant health check failed");

            return new HealthCheckResult
            {
                ServiceName = "Qdrant",
                IsHealthy = false,
                Message = $"Failed to connect: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<HealthCheckResult> CheckAzureOcrAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var httpClient = _httpClientFactory.CreateClient("AzureOcr");

            // Try to access the OCR endpoint
            // Note: We expect a 400/405 error when sending GET without image data - this is normal
            var response = await httpClient.GetAsync("/vision/v3.2/read/analyze", cancellationToken);

            stopwatch.Stop();

            // Status codes 400 or 405 indicate the service is running but expects different input
            // This is acceptable for a health check
            var isHealthy = response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                           response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                           response.IsSuccessStatusCode;

            if (isHealthy)
            {
                _logger.LogInformation("Azure OCR health check passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new HealthCheckResult
                {
                    ServiceName = "Azure OCR",
                    IsHealthy = true,
                    Message = "Successfully connected to Azure Computer Vision OCR",
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                _logger.LogWarning("Azure OCR health check returned unexpected status: {StatusCode}", response.StatusCode);

                return new HealthCheckResult
                {
                    ServiceName = "Azure OCR",
                    IsHealthy = false,
                    Message = $"Unexpected response: {response.StatusCode}",
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Azure OCR health check failed");

            return new HealthCheckResult
            {
                ServiceName = "Azure OCR",
                IsHealthy = false,
                Message = $"Failed to connect: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }
}
