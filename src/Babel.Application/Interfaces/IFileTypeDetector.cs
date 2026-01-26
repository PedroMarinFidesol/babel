using Babel.Domain.Enums;

namespace Babel.Application.Interfaces;

/// <summary>
/// Servicio para detectar el tipo de archivo basado en su extensión.
/// </summary>
public interface IFileTypeDetector
{
    /// <summary>
    /// Detecta el tipo de archivo basado en su extensión.
    /// </summary>
    /// <param name="fileName">Nombre del archivo con extensión</param>
    /// <returns>Tipo de archivo detectado</returns>
    FileExtensionType DetectFileType(string fileName);

    /// <summary>
    /// Determina si un archivo requiere procesamiento OCR.
    /// </summary>
    /// <param name="fileType">Tipo de archivo</param>
    /// <returns>True si requiere OCR</returns>
    bool RequiresOcr(FileExtensionType fileType);

    /// <summary>
    /// Obtiene el tipo MIME basado en la extensión del archivo.
    /// </summary>
    /// <param name="fileName">Nombre del archivo con extensión</param>
    /// <returns>Tipo MIME</returns>
    string GetMimeType(string fileName);

    /// <summary>
    /// Verifica si la extensión del archivo está soportada.
    /// </summary>
    /// <param name="fileName">Nombre del archivo con extensión</param>
    /// <returns>True si está soportada</returns>
    bool IsSupported(string fileName);
}
