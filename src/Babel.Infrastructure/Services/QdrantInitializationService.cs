using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio de inicialización que crea la colección de Qdrant al inicio de la aplicación.
/// </summary>
public class QdrantInitializationService : IHostedService
{
    private readonly QdrantClient _qdrantClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QdrantInitializationService> _logger;

    public QdrantInitializationService(
        QdrantClient qdrantClient,
        IConfiguration configuration,
        ILogger<QdrantInitializationService> logger)
    {
        _qdrantClient = qdrantClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collectionName = _configuration["Qdrant:CollectionName"] ?? "babel_documents";
        var vectorSize = _configuration.GetValue<int>("Qdrant:VectorSize", 1536);

        try
        {
            _logger.LogInformation("Verificando colección de Qdrant: {CollectionName}", collectionName);

            // Verificar si la colección existe
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);
            var collectionExists = collections.Any(c => c == collectionName);

            if (!collectionExists)
            {
                _logger.LogInformation(
                    "Creando colección {CollectionName} con tamaño de vector {VectorSize}",
                    collectionName,
                    vectorSize);

                await _qdrantClient.CreateCollectionAsync(
                    collectionName,
                    new VectorParams
                    {
                        Size = (ulong)vectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                // Crear índices de payload para filtrado eficiente
                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName,
                    "projectId",
                    PayloadSchemaType.Keyword,
                    cancellationToken: cancellationToken);

                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName,
                    "documentId",
                    PayloadSchemaType.Keyword,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Colección {CollectionName} creada exitosamente", collectionName);
            }
            else
            {
                _logger.LogInformation("Colección {CollectionName} ya existe", collectionName);

                // Verificar que la colección tiene la configuración correcta
                var collectionInfo = await _qdrantClient.GetCollectionInfoAsync(collectionName, cancellationToken);
                var currentVectorSize = collectionInfo.Config.Params.VectorsConfig.Params.Size;

                if ((int)currentVectorSize != vectorSize)
                {
                    _logger.LogWarning(
                        "La colección {CollectionName} tiene un tamaño de vector diferente. " +
                        "Esperado: {Expected}, Actual: {Actual}. " +
                        "Considera eliminar y recrear la colección.",
                        collectionName,
                        vectorSize,
                        currentVectorSize);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar la colección de Qdrant: {CollectionName}", collectionName);
            // No lanzamos la excepción para permitir que la aplicación arranque
            // incluso si Qdrant no está disponible
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
