namespace Babel.Application.DTOs;

public class HealthCheckResult
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
}
