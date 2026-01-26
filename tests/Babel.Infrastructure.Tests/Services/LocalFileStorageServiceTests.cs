using System.Text;
using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Babel.Infrastructure.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly LocalFileStorageService _service;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageServiceTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"babel_test_{Guid.NewGuid()}");
        _logger = Substitute.For<ILogger<LocalFileStorageService>>();

        var options = Options.Create(new FileStorageOptions
        {
            BasePath = _testBasePath,
            MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
            AllowedExtensions = new[] { ".txt", ".pdf", ".docx", ".png", ".jpg" },
            OverwriteExisting = false,
            EnableDeduplication = true
        });

        _service = new LocalFileStorageService(options, _logger);
    }

    public void Dispose()
    {
        // Limpiar directorio de tests
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_WithValidFile_ShouldSaveAndReturnRelativePath()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var relativePath = await _service.SaveFileAsync(stream, fileName, projectId);

        // Assert
        relativePath.Should().StartWith(projectId.ToString());
        relativePath.Should().Contain("test.txt");

        var fullPath = Path.Combine(_testBasePath, relativePath);
        File.Exists(fullPath).Should().BeTrue();

        var savedContent = await File.ReadAllTextAsync(fullPath);
        savedContent.Should().Be(content);
    }

    [Fact]
    public async Task SaveFileAsync_WithDuplicateName_ShouldCreateUniqueFileName()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fileName = "test.txt";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Content 1"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("Content 2"));

        // Act
        var path1 = await _service.SaveFileAsync(stream1, fileName, projectId);
        var path2 = await _service.SaveFileAsync(stream2, fileName, projectId);

        // Assert
        path1.Should().NotBe(path2);
        path1.Should().Contain("test.txt");
        path2.Should().Contain("test_"); // Should have timestamp suffix
    }

    [Fact]
    public async Task SaveFileAsync_WithDisallowedExtension_ShouldThrowException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fileName = "malware.exe";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act
        Func<Task> act = () => _service.SaveFileAsync(stream, fileName, projectId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*extensión*no está permitida*");
    }

    [Fact]
    public async Task GetFileAsync_WithExistingFile_ShouldReturnStream()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "Test content for reading";
        using var writeStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var relativePath = await _service.SaveFileAsync(writeStream, "read_test.txt", projectId);

        // Act
        await using var readStream = await _service.GetFileAsync(relativePath);
        using var reader = new StreamReader(readStream);
        var readContent = await reader.ReadToEndAsync();

        // Assert
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task GetFileAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var relativePath = $"{Guid.NewGuid()}/nonexistent.txt";

        // Act
        Func<Task> act = () => _service.GetFileAsync(relativePath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_ShouldDeleteFile()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content to delete"));
        var relativePath = await _service.SaveFileAsync(stream, "delete_test.txt", projectId);

        // Verify file exists
        var exists = await _service.ExistsAsync(relativePath);
        exists.Should().BeTrue();

        // Act
        await _service.DeleteFileAsync(relativePath);

        // Assert
        var existsAfterDelete = await _service.ExistsAsync(relativePath);
        existsAfterDelete.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ShouldNotThrow()
    {
        // Arrange
        var relativePath = $"{Guid.NewGuid()}/nonexistent.txt";

        // Act
        Func<Task> act = () => _service.DeleteFileAsync(relativePath);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var relativePath = await _service.SaveFileAsync(stream, "exists_test.txt", projectId);

        // Act
        var exists = await _service.ExistsAsync(relativePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var relativePath = $"{Guid.NewGuid()}/nonexistent.txt";

        // Act
        var exists = await _service.ExistsAsync(relativePath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileHashAsync_ShouldReturnConsistentHash()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "Content for hashing";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var relativePath = await _service.SaveFileAsync(stream, "hash_test.txt", projectId);

        // Act
        var hash1 = await _service.GetFileHashAsync(relativePath);
        var hash2 = await _service.GetFileHashAsync(relativePath);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA256 produces 64 hex characters
    }

    [Fact]
    public async Task GetFileSizeAsync_ShouldReturnCorrectSize()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "This is test content with known size.";
        var expectedSize = Encoding.UTF8.GetByteCount(content);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var relativePath = await _service.SaveFileAsync(stream, "size_test.txt", projectId);

        // Act
        var size = await _service.GetFileSizeAsync(relativePath);

        // Assert
        size.Should().Be(expectedSize);
    }

    [Fact]
    public async Task DeleteProjectFilesAsync_ShouldDeleteAllProjectFiles()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("File 1"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("File 2"));

        var path1 = await _service.SaveFileAsync(stream1, "file1.txt", projectId);
        var path2 = await _service.SaveFileAsync(stream2, "file2.txt", projectId);

        // Verify files exist
        (await _service.ExistsAsync(path1)).Should().BeTrue();
        (await _service.ExistsAsync(path2)).Should().BeTrue();

        // Act
        await _service.DeleteProjectFilesAsync(projectId);

        // Assert
        (await _service.ExistsAsync(path1)).Should().BeFalse();
        (await _service.ExistsAsync(path2)).Should().BeFalse();

        var projectDir = Path.Combine(_testBasePath, projectId.ToString());
        Directory.Exists(projectDir).Should().BeFalse();
    }

    [Fact]
    public async Task SaveFileAsync_WithPathTraversalAttempt_ShouldStoreInCorrectLocation()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var maliciousFileName = "../../../etc/passwd.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act
        var relativePath = await _service.SaveFileAsync(stream, maliciousFileName, projectId);

        // Assert
        // El archivo debe guardarse en el directorio del proyecto, no fuera
        relativePath.Should().StartWith(projectId.ToString());
        var fullPath = Path.Combine(_testBasePath, relativePath);
        fullPath.Should().StartWith(_testBasePath);
    }

    [Fact]
    public async Task ComputeHashAsync_ShouldCalculateCorrectHash()
    {
        // Arrange
        var content = "Test content for static hash";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await LocalFileStorageService.ComputeHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
        // El stream debe volver a la posición original
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldCreateBaseDirectoryIfNotExists()
    {
        // Arrange
        var newBasePath = Path.Combine(Path.GetTempPath(), $"babel_new_test_{Guid.NewGuid()}");
        var logger = Substitute.For<ILogger<LocalFileStorageService>>();
        var options = Options.Create(new FileStorageOptions { BasePath = newBasePath });

        try
        {
            // Act
            var service = new LocalFileStorageService(options, logger);

            // Assert
            Directory.Exists(newBasePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(newBasePath))
            {
                Directory.Delete(newBasePath, recursive: true);
            }
        }
    }
}
