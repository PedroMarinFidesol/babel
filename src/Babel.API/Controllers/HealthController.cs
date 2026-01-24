using Babel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Babel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IHealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health check endpoint called");

        var sqlServerHealth = await _healthCheckService.CheckSqlServerAsync(cancellationToken);
        var qdrantHealth = await _healthCheckService.CheckQdrantAsync(cancellationToken);
        var azureOcrHealth = await _healthCheckService.CheckAzureOcrAsync(cancellationToken);

        var response = new
        {
            Status = sqlServerHealth.IsHealthy && qdrantHealth.IsHealthy && azureOcrHealth.IsHealthy ? "Healthy" : "Unhealthy",
            Timestamp = DateTime.UtcNow,
            Services = new[]
            {
                new
                {
                    sqlServerHealth.ServiceName,
                    sqlServerHealth.IsHealthy,
                    sqlServerHealth.Message,
                    ResponseTimeMs = sqlServerHealth.ResponseTime.TotalMilliseconds
                },
                new
                {
                    qdrantHealth.ServiceName,
                    qdrantHealth.IsHealthy,
                    qdrantHealth.Message,
                    ResponseTimeMs = qdrantHealth.ResponseTime.TotalMilliseconds
                },
                new
                {
                    azureOcrHealth.ServiceName,
                    azureOcrHealth.IsHealthy,
                    azureOcrHealth.Message,
                    ResponseTimeMs = azureOcrHealth.ResponseTime.TotalMilliseconds
                }
            }
        };

        var statusCode = response.Status == "Healthy" ? 200 : 503;
        return StatusCode(statusCode, response);
    }

    [HttpGet("sql-server")]
    public async Task<IActionResult> GetSqlServer(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckSqlServerAsync(cancellationToken);
        var statusCode = result.IsHealthy ? 200 : 503;
        return StatusCode(statusCode, result);
    }

    [HttpGet("qdrant")]
    public async Task<IActionResult> GetQdrant(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckQdrantAsync(cancellationToken);
        var statusCode = result.IsHealthy ? 200 : 503;
        return StatusCode(statusCode, result);
    }

    [HttpGet("azure-ocr")]
    public async Task<IActionResult> GetAzureOcr(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckAzureOcrAsync(cancellationToken);
        var statusCode = result.IsHealthy ? 200 : 503;
        return StatusCode(statusCode, result);
    }
}
