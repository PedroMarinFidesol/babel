using System.Security.Cryptography;
using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Implementación de almacenamiento de archivos en sistema de archivos local/NAS.
/// </summary>
public sealed class LocalFileStorageService : IStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _basePath = Path.GetFullPath(_options.BasePath);

        // Crear directorio base si no existe
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created storage base directory: {BasePath}", _basePath);
        }
    }

    public async Task<string> SaveFileAsync(
        Stream content,
        string fileName,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // Sanitizar nombre de archivo para prevenir path traversal
        fileName = SanitizeFileName(fileName);

        // Validar extensión
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!IsExtensionAllowed(extension))
        {
            throw new InvalidOperationException(
                $"La extensión '{extension}' no está permitida. Extensiones permitidas: {string.Join(", ", _options.AllowedExtensions)}");
        }

        // Validar tamaño
        if (content.CanSeek && content.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"El archivo excede el tamaño máximo permitido de {_options.MaxFileSizeBytes / 1024 / 1024}MB.");
        }

        // Crear directorio del proyecto
        var projectDirectory = GetProjectDirectory(projectId);
        if (!Directory.Exists(projectDirectory))
        {
            Directory.CreateDirectory(projectDirectory);
            _logger.LogDebug("Created project directory: {ProjectDirectory}", projectDirectory);
        }

        // Generar nombre único si es necesario
        var finalFileName = GetUniqueFileName(projectDirectory, fileName);
        var relativePath = Path.Combine(projectId.ToString(), finalFileName);
        var fullPath = Path.Combine(_basePath, relativePath);

        // Guardar archivo
        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation(
            "File saved: {FileName} -> {RelativePath} ({Size} bytes)",
            fileName, relativePath, fileStream.Length);

        return relativePath;
    }

    public async Task<Stream> GetFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"El archivo no existe: {filePath}", filePath);
        }

        // Devolver stream abierto para lectura
        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return await Task.FromResult(stream);
    }

    public async Task DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = GetFullPath(filePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {FilePath}", filePath);
        }
        else
        {
            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
        }

        await Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = GetFullPath(filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<string> GetFileHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"El archivo no existe: {filePath}", filePath);
        }

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public Task<long> GetFileSizeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"El archivo no existe: {filePath}", filePath);
        }

        var fileInfo = new FileInfo(fullPath);
        return Task.FromResult(fileInfo.Length);
    }

    public async Task DeleteProjectFilesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var projectDirectory = GetProjectDirectory(projectId);

        if (Directory.Exists(projectDirectory))
        {
            Directory.Delete(projectDirectory, recursive: true);
            _logger.LogInformation("Deleted project directory: {ProjectDirectory}", projectDirectory);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Calcula el hash SHA256 de un stream sin modificar su posición.
    /// Útil para calcular el hash antes de guardar.
    /// </summary>
    public static async Task<string> ComputeHashAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var originalPosition = content.CanSeek ? content.Position : 0;

        try
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var hashBytes = await SHA256.HashDataAsync(content, cancellationToken);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        finally
        {
            if (content.CanSeek)
            {
                content.Position = originalPosition;
            }
        }
    }

    private string GetProjectDirectory(Guid projectId)
    {
        return Path.Combine(_basePath, projectId.ToString());
    }

    private string GetFullPath(string relativePath)
    {
        // Prevenir path traversal
        var normalizedPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        if (!normalizedPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Intento de acceso fuera del directorio base.");
        }

        return normalizedPath;
    }

    private string GetUniqueFileName(string directory, string fileName)
    {
        if (_options.OverwriteExisting)
        {
            return fileName;
        }

        var fullPath = Path.Combine(directory, fileName);

        if (!File.Exists(fullPath))
        {
            return fileName;
        }

        // Generar nombre único con timestamp
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssff");

        return $"{nameWithoutExtension}_{timestamp}{extension}";
    }

    private bool IsExtensionAllowed(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return _options.AllowedExtensions
            .Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sanitiza el nombre de archivo para prevenir path traversal y caracteres inválidos.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Obtener solo el nombre del archivo, eliminando cualquier ruta
        var sanitized = Path.GetFileName(fileName);

        // Si después de sanitizar queda vacío, generar un nombre único
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"file_{Guid.NewGuid():N}";
        }

        // Reemplazar caracteres no válidos
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }
}
