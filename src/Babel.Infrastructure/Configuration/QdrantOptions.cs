using System.ComponentModel.DataAnnotations;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para Qdrant.
/// </summary>
public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    [Required]
    [MinLength(1)]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int GrpcPort { get; set; } = 6334;

    [Range(1, 65535)]
    public int HttpPort { get; set; } = 6333;

    [Required]
    [MinLength(1)]
    public string CollectionName { get; set; } = "babel_documents";

    [Range(1, 10000)]
    public int VectorSize { get; set; } = 1536;
}
