namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para Hangfire.
/// </summary>
public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Ruta del dashboard de Hangfire.
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// Número de workers para procesar jobs.
    /// </summary>
    public int WorkerCount { get; set; } = 2;

    /// <summary>
    /// Intervalo de polling en segundos para detectar nuevos jobs.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Número máximo de reintentos para jobs fallidos.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
