using System.ComponentModel.DataAnnotations;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para Azure Computer Vision (OCR).
/// </summary>
public class AzureOcrOptions
{
    public const string SectionName = "AzureComputerVision";

    [Required]
    [Url]
    public string Endpoint { get; set; } = "http://localhost:5000/";

    public string ApiKey { get; set; } = string.Empty;

    public bool UseLocalContainer { get; set; } = true;
}
