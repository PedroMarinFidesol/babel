namespace Babel.Application.Common;

/// <summary>
/// Errores de dominio predefinidos organizados por entidad.
/// </summary>
public static class DomainErrors
{
    public static class Project
    {
        public static readonly Error NotFound = new(
            "Project.NotFound",
            "El proyecto no fue encontrado.");

        public static readonly Error NameAlreadyExists = new(
            "Project.NameAlreadyExists",
            "Ya existe un proyecto con ese nombre.");

        public static readonly Error NameRequired = new(
            "Project.NameRequired",
            "El nombre del proyecto es requerido.");

        public static readonly Error NameTooLong = new(
            "Project.NameTooLong",
            "El nombre del proyecto excede los 200 caracteres.");

        public static readonly Error DescriptionTooLong = new(
            "Project.DescriptionTooLong",
            "La descripción del proyecto excede los 2000 caracteres.");

        public static readonly Error HasDocuments = new(
            "Project.HasDocuments",
            "No se puede eliminar un proyecto que contiene documentos.");
    }

    public static class Document
    {
        public static readonly Error NotFound = new(
            "Document.NotFound",
            "El documento no fue encontrado.");

        public static readonly Error ProjectNotFound = new(
            "Document.ProjectNotFound",
            "El proyecto asociado al documento no existe.");

        public static readonly Error FileNameRequired = new(
            "Document.FileNameRequired",
            "El nombre del archivo es requerido.");

        public static readonly Error FileTooLarge = new(
            "Document.FileTooLarge",
            "El archivo excede el tamaño máximo permitido.");

        public static readonly Error UnsupportedFileType = new(
            "Document.UnsupportedFileType",
            "El tipo de archivo no está soportado.");

        public static readonly Error DuplicateFile = new(
            "Document.DuplicateFile",
            "Ya existe un archivo idéntico en este proyecto.");

        public static readonly Error EmptyFile = new(
            "Document.EmptyFile",
            "El archivo está vacío.");

        public static readonly Error ProcessingFailed = new(
            "Document.ProcessingFailed",
            "El procesamiento del documento ha fallado.");

        public static readonly Error NotReadyForVectorization = new(
            "Document.NotReadyForVectorization",
            "El documento no está listo para vectorización.");

        public static readonly Error RequiresOcr = new(
            "Document.RequiresOcr",
            "El documento requiere procesamiento OCR.");

        public static readonly Error ExtractionFailed = new(
            "Document.ExtractionFailed",
            "Error al extraer texto del documento.");
    }

    public static class DocumentChunk
    {
        public static readonly Error NotFound = new(
            "DocumentChunk.NotFound",
            "El fragmento de documento no fue encontrado.");

        public static readonly Error DocumentNotFound = new(
            "DocumentChunk.DocumentNotFound",
            "El documento padre no fue encontrado.");

        public static readonly Error EmptyContent = new(
            "DocumentChunk.EmptyContent",
            "El contenido del fragmento no puede estar vacío.");
    }

    public static class Vectorization
    {
        public static readonly Error ChunkingFailed = new(
            "Vectorization.ChunkingFailed",
            "Error al dividir el documento en fragmentos.");

        public static readonly Error EmbeddingFailed = new(
            "Vectorization.EmbeddingFailed",
            "Error al generar embeddings vectoriales.");

        public static readonly Error QdrantOperationFailed = new(
            "Vectorization.QdrantOperationFailed",
            "Error en operación con la base de datos vectorial.");

        public static readonly Error DocumentNotReady = new(
            "Vectorization.DocumentNotReady",
            "El documento no está listo para vectorización.");

        public static readonly Error EmptyContent = new(
            "Vectorization.EmptyContent",
            "El documento no tiene contenido para vectorizar.");

        public static readonly Error ProviderNotConfigured = new(
            "Vectorization.ProviderNotConfigured",
            "El proveedor de embeddings no está configurado correctamente.");
    }

    public static class Chat
    {
        public static readonly Error ProjectNotFound = new(
            "Chat.ProjectNotFound",
            "El proyecto no fue encontrado.");

        public static readonly Error NoVectorizedDocuments = new(
            "Chat.NoVectorizedDocuments",
            "El proyecto no tiene documentos procesados para consulta.");

        public static readonly Error MessageRequired = new(
            "Chat.MessageRequired",
            "El mensaje de consulta es requerido.");

        public static readonly Error MessageTooLong = new(
            "Chat.MessageTooLong",
            "El mensaje excede la longitud máxima permitida.");

        public static readonly Error EmbeddingFailed = new(
            "Chat.EmbeddingFailed",
            "Error al generar embedding de la consulta.");

        public static readonly Error SearchFailed = new(
            "Chat.SearchFailed",
            "Error en la búsqueda de documentos relevantes.");

        public static readonly Error LlmFailed = new(
            "Chat.LlmFailed",
            "Error al generar respuesta del modelo de lenguaje.");
    }
}
