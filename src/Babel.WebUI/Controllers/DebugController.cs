using Babel.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Babel.Application.Interfaces;
using Babel.Application.Common;

namespace Babel.WebUI.Controllers;

/// <summary>
/// Controlador de debug para diagnosticar problemas.
/// </summary>
[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly QdrantClient _qdrantClient;
    private readonly QdrantOptions _qdrantOptions;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        QdrantClient qdrantClient,
        IOptions<QdrantOptions> qdrantOptions,
        IApplicationDbContext dbContext,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<DebugController> logger)
    {
        _qdrantClient = qdrantClient;
        _qdrantOptions = qdrantOptions.Value;
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    /// <summary>
    /// Verifica el estado de Qdrant y la colección.
    /// </summary>
    [HttpGet("qdrant-status")]
    public async Task<IActionResult> GetQdrantStatus(CancellationToken cancellationToken)
    {
        try
        {
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);

            var collectionInfo = new
            {
                configuredCollection = _qdrantOptions.CollectionName,
                configuredHost = _qdrantOptions.Host,
                configuredGrpcPort = _qdrantOptions.GrpcPort,
                configuredVectorSize = _qdrantOptions.VectorSize,
                existingCollections = collections.ToList()
            };

            // Verificar si nuestra colección existe
            if (collections.Contains(_qdrantOptions.CollectionName))
            {
                var info = await _qdrantClient.GetCollectionInfoAsync(_qdrantOptions.CollectionName, cancellationToken);

                return Ok(new
                {
                    status = "connected",
                    collection = collectionInfo,
                    collectionDetails = new
                    {
                        pointsCount = info.PointsCount,
                        status = info.Status.ToString(),
                        vectorSize = info.Config?.Params?.VectorsConfig?.Params?.Size
                    }
                });
            }

            return Ok(new
            {
                status = "connected",
                warning = $"Collection '{_qdrantOptions.CollectionName}' not found",
                collection = collectionInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to Qdrant");
            return Ok(new
            {
                status = "error",
                error = ex.Message,
                configuredHost = _qdrantOptions.Host,
                configuredGrpcPort = _qdrantOptions.GrpcPort
            });
        }
    }

    /// <summary>
    /// Lista los puntos de Qdrant para un proyecto específico.
    /// </summary>
    [HttpGet("qdrant-points/{projectId:guid}")]
    public async Task<IActionResult> GetQdrantPoints(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "project_id",
                            Match = new Match { Keyword = projectId.ToString() }
                        }
                    }
                }
            };

            var scrollResult = await _qdrantClient.ScrollAsync(
                collectionName: _qdrantOptions.CollectionName,
                filter: filter,
                limit: 100,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            var points = scrollResult.Result.Select(p => new
            {
                id = p.Id.Uuid,
                documentId = p.Payload.TryGetValue("document_id", out var docId) ? docId.StringValue : null,
                projectId = p.Payload.TryGetValue("project_id", out var projId) ? projId.StringValue : null,
                chunkIndex = p.Payload.TryGetValue("chunk_index", out var idx) ? idx.IntegerValue : 0,
                fileName = p.Payload.TryGetValue("file_name", out var fn) ? fn.StringValue : null
            }).ToList();

            return Ok(new
            {
                collection = _qdrantOptions.CollectionName,
                projectId = projectId.ToString(),
                pointsFound = points.Count,
                points
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Qdrant points for project {ProjectId}", projectId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Verifica el estado de documentos en la BD.
    /// </summary>
    [HttpGet("documents/{projectId:guid}")]
    public async Task<IActionResult> GetDocumentsStatus(Guid projectId, CancellationToken cancellationToken)
    {
        var documents = await _dbContext.Documents
            .Where(d => d.ProjectId == projectId)
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.Status,
                d.IsVectorized,
                d.VectorizedAt,
                contentLength = d.Content != null ? d.Content.Length : 0,
                chunksCount = d.Chunks.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            projectId,
            totalDocuments = documents.Count,
            vectorizedDocuments = documents.Count(d => d.IsVectorized),
            documents
        });
    }

    /// <summary>
    /// Lista todas las colecciones y sus puntos (para debug).
    /// </summary>
    [HttpGet("qdrant-all-points")]
    public async Task<IActionResult> GetAllQdrantPoints(CancellationToken cancellationToken)
    {
        try
        {
            var scrollResult = await _qdrantClient.ScrollAsync(
                collectionName: _qdrantOptions.CollectionName,
                limit: 100,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            var points = scrollResult.Result.Select(p => new
            {
                id = p.Id.Uuid,
                documentId = p.Payload.TryGetValue("document_id", out var docId) ? docId.StringValue : null,
                projectId = p.Payload.TryGetValue("project_id", out var projId) ? projId.StringValue : null,
                chunkIndex = p.Payload.TryGetValue("chunk_index", out var idx) ? idx.IntegerValue : 0,
                fileName = p.Payload.TryGetValue("file_name", out var fn) ? fn.StringValue : null
            }).ToList();

            // Agrupar por projectId
            var byProject = points.GroupBy(p => p.projectId).Select(g => new
            {
                projectId = g.Key,
                count = g.Count(),
                files = g.Select(p => p.fileName).Distinct().ToList()
            });

            return Ok(new
            {
                collection = _qdrantOptions.CollectionName,
                totalPoints = points.Count,
                byProject
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all Qdrant points");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Prueba la búsqueda vectorial completa.
    /// </summary>
    [HttpPost("test-search/{projectId:guid}")]
    public async Task<IActionResult> TestSearch(
        Guid projectId,
        [FromBody] TestSearchRequest request,
        CancellationToken cancellationToken)
    {
        var steps = new List<object>();

        // 1. Generar embedding
        steps.Add(new { step = 1, action = "Generating embedding", query = request.Query });

        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);

        if (embeddingResult.IsFailure)
        {
            steps.Add(new { step = 1, status = "FAILED", error = embeddingResult.Error.Description });
            return Ok(new { success = false, steps });
        }

        var embedding = embeddingResult.Value;
        steps.Add(new {
            step = 1,
            status = "OK",
            embeddingDimensions = embedding.Length,
            firstValues = embedding.ToArray().Take(5).ToArray()
        });

        // 2. Buscar en Qdrant con diferentes scores
        var minScores = new[] { 0.0f, 0.3f, 0.5f, 0.7f };

        foreach (var minScore in minScores)
        {
            var searchResult = await _vectorStoreService.SearchAsync(
                embedding,
                projectId,
                topK: 5,
                minScore: minScore,
                cancellationToken);

            if (searchResult.IsFailure)
            {
                steps.Add(new {
                    step = 2,
                    minScore,
                    status = "FAILED",
                    error = searchResult.Error.Description
                });
            }
            else
            {
                steps.Add(new {
                    step = 2,
                    minScore,
                    status = "OK",
                    resultsCount = searchResult.Value.Count,
                    results = searchResult.Value.Select(r => new {
                        r.FileName,
                        r.ChunkIndex,
                        r.SimilarityScore
                    })
                });
            }
        }

        return Ok(new { success = true, steps });
    }

    /// <summary>
    /// Verifica los chunks en la BD para un documento.
    /// </summary>
    [HttpGet("chunks/{documentId:guid}")]
    public async Task<IActionResult> GetChunks(Guid documentId, CancellationToken cancellationToken)
    {
        var chunks = await _dbContext.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .Select(c => new
            {
                c.Id,
                c.DocumentId,
                c.ChunkIndex,
                c.QdrantPointId,
                contentLength = c.Content.Length,
                contentPreview = c.Content.Substring(0, Math.Min(100, c.Content.Length))
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            documentId,
            chunksInDatabase = chunks.Count,
            chunks
        });
    }

    /// <summary>
    /// Verifica todos los chunks de un proyecto.
    /// </summary>
    [HttpGet("all-chunks/{projectId:guid}")]
    public async Task<IActionResult> GetAllChunks(Guid projectId, CancellationToken cancellationToken)
    {
        var chunks = await _dbContext.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Document.ProjectId == projectId)
            .Select(c => new
            {
                c.Id,
                c.DocumentId,
                fileName = c.Document.FileName,
                c.ChunkIndex,
                c.QdrantPointId
            })
            .ToListAsync(cancellationToken);

        var byDocument = chunks.GroupBy(c => c.fileName).Select(g => new
        {
            fileName = g.Key,
            chunksCount = g.Count(),
            chunkIndices = g.Select(c => c.ChunkIndex).OrderBy(i => i).ToList()
        });

        return Ok(new
        {
            projectId,
            totalChunksInDatabase = chunks.Count,
            byDocument
        });
    }
}

public class TestSearchRequest
{
    public string Query { get; set; } = "";
}
