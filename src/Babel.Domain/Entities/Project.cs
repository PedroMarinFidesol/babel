namespace Babel.Domain.Entities;

/// <summary>
/// Representa un proyecto que agrupa documentos relacionados.
/// Los documentos se almacenan físicamente en NAS y se vectorizan en Qdrant para búsqueda semántica.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// Nombre del proyecto.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional del proyecto.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Colección de documentos asociados al proyecto.
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
