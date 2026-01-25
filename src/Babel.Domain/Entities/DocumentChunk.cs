namespace Babel.Domain.Entities;

/// <summary>
/// Representa un fragmento (chunk) de un documento para vectorización.
/// Cada chunk se almacena como un punto individual en Qdrant para búsqueda semántica.
/// </summary>
public class DocumentChunk : BaseEntity
{
    #region Relación con Documento

    /// <summary>
    /// ID del documento al que pertenece este chunk.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Documento al que pertenece este chunk.
    /// </summary>
    public Document Document { get; set; } = null!;

    #endregion

    #region Posición del Chunk

    /// <summary>
    /// Índice del chunk dentro del documento (0, 1, 2, ...).
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Posición del carácter inicial del chunk en el contenido original del documento.
    /// </summary>
    public int StartCharIndex { get; set; }

    /// <summary>
    /// Posición del carácter final del chunk en el contenido original del documento.
    /// </summary>
    public int EndCharIndex { get; set; }

    #endregion

    #region Contenido del Chunk

    /// <summary>
    /// Contenido textual del chunk.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Número estimado de tokens en el chunk.
    /// </summary>
    public int TokenCount { get; set; }

    #endregion

    #region Referencia a Qdrant

    /// <summary>
    /// ID del punto en Qdrant donde se almacena el embedding de este chunk.
    /// </summary>
    public Guid QdrantPointId { get; set; }

    #endregion

    #region Metadatos Opcionales

    /// <summary>
    /// Número de página donde se encuentra el chunk (si aplica, ej: PDFs).
    /// </summary>
    public string? PageNumber { get; set; }

    /// <summary>
    /// Título de la sección donde se encuentra el chunk (si se detecta).
    /// </summary>
    public string? SectionTitle { get; set; }

    #endregion
}
