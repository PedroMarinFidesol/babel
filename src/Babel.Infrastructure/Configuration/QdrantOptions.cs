using System.ComponentModel.DataAnnotations;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para Qdrant.
/// </summary>
public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    [Required]
    [Url]
    public string Endpoint { get; set; } = "http://localhost:6333";

    [Required]
    [MinLength(1)]
    public string CollectionName { get; set; } = "babel_documents";

    [Range(1, 10000)]
    public int VectorSize { get; set; } = 1536;
}
