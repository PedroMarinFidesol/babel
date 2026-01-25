using Babel.Domain.Enums;

namespace Babel.Domain.Entities;

/// <summary>
/// Representa un documento almacenado en el sistema.
/// El archivo físico se almacena en NAS y el contenido se vectoriza en Qdrant mediante chunks.
/// </summary>
public class Document : BaseEntity
{
    #region Relación con Proyecto

    /// <summary>
    /// ID del proyecto al que pertenece el documento.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Proyecto al que pertenece el documento.
    /// </summary>
    public Project Project { get; set; } = null!;

    #endregion

    #region Información del Archivo Físico

    /// <summary>
    /// Nombre original del archivo (ej: "informe_2024.pdf").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Extensión del archivo incluyendo el punto (ej: ".pdf").
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Ruta relativa del archivo en el NAS (ej: "projects/guid/archivo.pdf").
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Hash SHA256 del contenido del archivo para detectar duplicados.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Tipo MIME del archivo (ej: "application/pdf", "image/png").
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    #endregion

    #region Clasificación

    /// <summary>
    /// Tipo de archivo basado en la extensión (TextBased, ImageBased, Pdf, etc.).
    /// </summary>
    public FileExtensionType FileType { get; set; } = FileExtensionType.Unknown;

    #endregion

    #region Estado de Procesamiento

    /// <summary>
    /// Estado actual del procesamiento del documento.
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>
    /// Indica si el documento requiere procesamiento OCR.
    /// </summary>
    public bool RequiresOcr { get; set; }

    /// <summary>
    /// Indica si el resultado del OCR ha sido revisado por un usuario.
    /// </summary>
    public bool OcrReviewed { get; set; }

    /// <summary>
    /// Fecha y hora en que se completó el procesamiento del documento.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    #endregion

    #region Contenido Extraído

    /// <summary>
    /// Texto extraído del documento (mediante OCR o extracción directa).
    /// </summary>
    public string? Content { get; set; }

    #endregion

    #region Vectorización

    /// <summary>
    /// Indica si el documento ha sido vectorizado y sus chunks están en Qdrant.
    /// </summary>
    public bool IsVectorized { get; set; }

    /// <summary>
    /// Fecha y hora en que se completó la vectorización.
    /// </summary>
    public DateTime? VectorizedAt { get; set; }

    #endregion

    #region Navegación

    /// <summary>
    /// Chunks del documento para vectorización en Qdrant.
    /// </summary>
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();

    #endregion
}
