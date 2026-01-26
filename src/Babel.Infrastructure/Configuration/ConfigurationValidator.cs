using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Valida la configuración de la aplicación al inicio.
/// </summary>
public class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    public ConfigurationValidator(IConfiguration configuration, ILogger<ConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool Validate()
    {
        _errors.Clear();
        _warnings.Clear();

        ValidateConnectionStrings();
        ValidateQdrant();
        ValidateFileStorage();
        ValidateSemanticKernel();
        ValidateAzureOcr();

        // Log warnings
        foreach (var warning in _warnings)
        {
            _logger.LogWarning("Configuración: {Warning}", warning);
        }

        // Log errors
        foreach (var error in _errors)
        {
            _logger.LogError("Configuración: {Error}", error);
        }

        if (_errors.Count > 0)
        {
            _logger.LogError(
                "Se encontraron {ErrorCount} errores de configuración. La aplicación podría no funcionar correctamente.",
                _errors.Count);
            return false;
        }

        _logger.LogInformation("Validación de configuración completada exitosamente");
        return true;
    }

    private void ValidateConnectionStrings()
    {
        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            _errors.Add("ConnectionStrings:DefaultConnection no está configurado");
        }

        var hangfireConnection = _configuration.GetConnectionString("HangfireConnection");

        if (string.IsNullOrWhiteSpace(hangfireConnection))
        {
            _warnings.Add("ConnectionStrings:HangfireConnection no está configurado. Se usará DefaultConnection para Hangfire.");
        }
    }

    private void ValidateQdrant()
    {
        var host = _configuration["Qdrant:Host"];

        if (string.IsNullOrWhiteSpace(host))
        {
            _warnings.Add("Qdrant:Host no está configurado. Se usará 'localhost'.");
        }

        var grpcPort = _configuration.GetValue<int>("Qdrant:GrpcPort");

        if (grpcPort <= 0 || grpcPort > 65535)
        {
            _warnings.Add("Qdrant:GrpcPort no está configurado o es inválido. Se usará 6334.");
        }

        var collectionName = _configuration["Qdrant:CollectionName"];

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            _warnings.Add("Qdrant:CollectionName no está configurado. Se usará 'babel_documents'.");
        }

        var vectorSize = _configuration.GetValue<int>("Qdrant:VectorSize");

        if (vectorSize <= 0)
        {
            _warnings.Add("Qdrant:VectorSize no está configurado. Se usará 1536 (OpenAI ada-002).");
        }
    }

    private void ValidateFileStorage()
    {
        var basePath = _configuration["FileStorage:BasePath"];

        if (string.IsNullOrWhiteSpace(basePath))
        {
            _warnings.Add("FileStorage:BasePath no está configurado. Se usará './uploads'.");
        }

        var maxFileSize = _configuration.GetValue<long>("FileStorage:MaxFileSizeBytes");

        if (maxFileSize <= 0)
        {
            _warnings.Add("FileStorage:MaxFileSizeBytes no está configurado. Se usará 100MB.");
        }
    }

    private void ValidateSemanticKernel()
    {
        var defaultProvider = _configuration["SemanticKernel:DefaultProvider"];

        if (string.IsNullOrWhiteSpace(defaultProvider))
        {
            _warnings.Add("SemanticKernel:DefaultProvider no está configurado. Se usará 'Ollama'.");
            defaultProvider = "Ollama";
        }

        switch (defaultProvider.ToLowerInvariant())
        {
            case "ollama":
                var ollamaEndpoint = _configuration["SemanticKernel:Ollama:Endpoint"];
                if (string.IsNullOrWhiteSpace(ollamaEndpoint))
                {
                    _warnings.Add("SemanticKernel:Ollama:Endpoint no está configurado. Se usará 'http://localhost:11434'.");
                }
                break;

            case "openai":
                var openaiApiKey = _configuration["SemanticKernel:OpenAI:ApiKey"];
                if (string.IsNullOrWhiteSpace(openaiApiKey))
                {
                    _errors.Add("SemanticKernel:OpenAI:ApiKey es requerido cuando el proveedor es OpenAI.");
                }
                break;

            case "gemini":
                var geminiApiKey = _configuration["SemanticKernel:Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(geminiApiKey))
                {
                    _errors.Add("SemanticKernel:Gemini:ApiKey es requerido cuando el proveedor es Gemini.");
                }
                break;

            default:
                _errors.Add($"SemanticKernel:DefaultProvider tiene un valor no soportado: {defaultProvider}");
                break;
        }
    }

    private void ValidateAzureOcr()
    {
        var useLocalContainer = _configuration.GetValue<bool>("AzureComputerVision:UseLocalContainer");

        if (!useLocalContainer)
        {
            var endpoint = _configuration["AzureComputerVision:Endpoint"];
            var apiKey = _configuration["AzureComputerVision:ApiKey"];

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _errors.Add("AzureComputerVision:Endpoint es requerido cuando no se usa el contenedor local.");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _errors.Add("AzureComputerVision:ApiKey es requerido cuando no se usa el contenedor local.");
            }
        }
    }

    public IReadOnlyList<string> Errors => _errors.AsReadOnly();
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();
}
