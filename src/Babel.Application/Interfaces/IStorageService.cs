namespace Babel.Application.Interfaces;

/// <summary>
/// Servicio de almacenamiento de archivos.
/// Abstracción que permite usar diferentes proveedores (local, Azure Blob, S3, etc.)
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Guarda un archivo en el almacenamiento.
    /// </summary>
    /// <param name="content">Stream con el contenido del archivo</param>
    /// <param name="fileName">Nombre original del archivo</param>
    /// <param name="projectId">ID del proyecto al que pertenece</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Ruta relativa donde se guardó el archivo</returns>
    Task<string> SaveFileAsync(
        Stream content,
        string fileName,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un archivo del almacenamiento.
    /// </summary>
    /// <param name="filePath">Ruta relativa del archivo</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Stream con el contenido del archivo</returns>
    Task<Stream> GetFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un archivo del almacenamiento.
    /// </summary>
    /// <param name="filePath">Ruta relativa del archivo</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un archivo existe en el almacenamiento.
    /// </summary>
    /// <param name="filePath">Ruta relativa del archivo</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si el archivo existe</returns>
    Task<bool> ExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula el hash SHA256 de un archivo.
    /// </summary>
    /// <param name="filePath">Ruta relativa del archivo</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Hash SHA256 en formato hexadecimal</returns>
    Task<string> GetFileHashAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el tamaño de un archivo en bytes.
    /// </summary>
    /// <param name="filePath">Ruta relativa del archivo</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Tamaño en bytes</returns>
    Task<long> GetFileSizeAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina todos los archivos de un proyecto.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task DeleteProjectFilesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
