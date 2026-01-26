using Babel.Application.Interfaces;
using Babel.Domain.Enums;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Implementación del detector de tipo de archivo basado en extensión.
/// </summary>
public sealed class FileTypeDetectorService : IFileTypeDetector
{
    private static readonly Dictionary<string, FileExtensionType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // TextBased
        { ".txt", FileExtensionType.TextBased },
        { ".md", FileExtensionType.TextBased },
        { ".markdown", FileExtensionType.TextBased },
        { ".json", FileExtensionType.TextBased },
        { ".xml", FileExtensionType.TextBased },
        { ".csv", FileExtensionType.TextBased },
        { ".html", FileExtensionType.TextBased },
        { ".htm", FileExtensionType.TextBased },
        { ".yaml", FileExtensionType.TextBased },
        { ".yml", FileExtensionType.TextBased },
        { ".log", FileExtensionType.TextBased },
        { ".ini", FileExtensionType.TextBased },
        { ".cfg", FileExtensionType.TextBased },

        // ImageBased
        { ".jpg", FileExtensionType.ImageBased },
        { ".jpeg", FileExtensionType.ImageBased },
        { ".png", FileExtensionType.ImageBased },
        { ".gif", FileExtensionType.ImageBased },
        { ".bmp", FileExtensionType.ImageBased },
        { ".tiff", FileExtensionType.ImageBased },
        { ".tif", FileExtensionType.ImageBased },
        { ".webp", FileExtensionType.ImageBased },

        // Pdf
        { ".pdf", FileExtensionType.Pdf },

        // OfficeDocument
        { ".doc", FileExtensionType.OfficeDocument },
        { ".docx", FileExtensionType.OfficeDocument },
        { ".xls", FileExtensionType.OfficeDocument },
        { ".xlsx", FileExtensionType.OfficeDocument },
        { ".ppt", FileExtensionType.OfficeDocument },
        { ".pptx", FileExtensionType.OfficeDocument },
        { ".odt", FileExtensionType.OfficeDocument },
        { ".ods", FileExtensionType.OfficeDocument },
        { ".odp", FileExtensionType.OfficeDocument },
        { ".rtf", FileExtensionType.OfficeDocument }
    };

    private static readonly Dictionary<string, string> MimeTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Text
        { ".txt", "text/plain" },
        { ".md", "text/markdown" },
        { ".markdown", "text/markdown" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".csv", "text/csv" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".yaml", "text/yaml" },
        { ".yml", "text/yaml" },
        { ".log", "text/plain" },
        { ".ini", "text/plain" },
        { ".cfg", "text/plain" },

        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".tiff", "image/tiff" },
        { ".tif", "image/tiff" },
        { ".webp", "image/webp" },

        // PDF
        { ".pdf", "application/pdf" },

        // Office
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".odt", "application/vnd.oasis.opendocument.text" },
        { ".ods", "application/vnd.oasis.opendocument.spreadsheet" },
        { ".odp", "application/vnd.oasis.opendocument.presentation" },
        { ".rtf", "application/rtf" }
    };

    public FileExtensionType DetectFileType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return FileExtensionType.Unknown;
        }

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return FileExtensionType.Unknown;
        }

        return ExtensionMap.TryGetValue(extension, out var fileType)
            ? fileType
            : FileExtensionType.Unknown;
    }

    public bool RequiresOcr(FileExtensionType fileType)
    {
        // Las imágenes siempre requieren OCR
        // Los PDFs pueden requerir OCR si son escaneados (esto se determinaría después de analizar el PDF)
        // Por ahora, marcamos los PDFs como que requieren OCR, y luego el procesador determinará si realmente lo necesita
        return fileType switch
        {
            FileExtensionType.ImageBased => true,
            FileExtensionType.Pdf => true, // Asumir que puede necesitar OCR, verificar después
            _ => false
        };
    }

    public string GetMimeType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "application/octet-stream";
        }

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return "application/octet-stream";
        }

        return MimeTypeMap.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }

    public bool IsSupported(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return ExtensionMap.ContainsKey(extension);
    }
}
