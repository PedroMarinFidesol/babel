using System.ComponentModel.DataAnnotations;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para almacenamiento de archivos.
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    public string Provider { get; set; } = "Local";

    [Required]
    [MinLength(1)]
    public string BasePath { get; set; } = "./uploads";

    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; set; } = 104857600; // 100 MB

    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf", ".docx", ".xlsx", ".txt", ".md",
        ".json", ".xml", ".png", ".jpg", ".jpeg",
        ".tiff", ".bmp"
    ];

    /// <summary>
    /// Si es true, calcula el hash SHA256 al guardar para detectar duplicados.
    /// </summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>
    /// Si es true, sobrescribe archivos existentes con el mismo nombre.
    /// Si es false, añade un sufijo único al nombre.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}
