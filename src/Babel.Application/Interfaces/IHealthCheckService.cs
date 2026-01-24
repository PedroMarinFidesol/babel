using Babel.Application.DTOs;

namespace Babel.Application.Interfaces;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckSqlServerAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckQdrantAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckAzureOcrAsync(CancellationToken cancellationToken = default);
}
