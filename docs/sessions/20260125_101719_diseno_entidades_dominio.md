# Sesión 2026-01-25 10:17 - Diseño de Entidades de Dominio

## Resumen de la Sesión

Se diseñaron e implementaron las entidades de dominio mejoradas para gestionar proyectos y documentos, incluyendo una nueva entidad `DocumentChunk` para soportar el patrón de chunking necesario para RAG con Qdrant.

## Cambios Implementados

### 1. Entidad Project Mejorada

**Archivo:** `src/Babel.Domain/Entities/Project.cs`

```csharp
public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
```

**Cambios:**
- Añadido campo `Description` opcional para descripción del proyecto

### 2. Entidad Document Mejorada

**Archivo:** `src/Babel.Domain/Entities/Document.cs`

```csharp
public class Document : BaseEntity
{
    // Relación con proyecto
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Información del archivo físico
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;

    // Clasificación
    public FileExtensionType FileType { get; set; } = FileExtensionType.Unknown;

    // Estado de procesamiento
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public bool RequiresOcr { get; set; }
    public bool OcrReviewed { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Contenido extraído
    public string? Content { get; set; }

    // Vectorización
    public bool IsVectorized { get; set; }
    public DateTime? VectorizedAt { get; set; }

    // Navegación a chunks
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
```

**Nuevos campos:**
- `FileSizeBytes` - Tamaño del archivo en bytes
- `ContentHash` - SHA256 para detectar duplicados
- `MimeType` - Tipo MIME del archivo
- `FileType` - Clasificación por tipo de extensión
- `IsVectorized` - Indica si está vectorizado en Qdrant
- `VectorizedAt` - Fecha de vectorización
- `Chunks` - Colección de chunks para vectorización

### 3. Nueva Entidad DocumentChunk

**Archivo:** `src/Babel.Domain/Entities/DocumentChunk.cs`

```csharp
public class DocumentChunk : BaseEntity
{
    // Relación con documento
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    // Posición del chunk
    public int ChunkIndex { get; set; }
    public int StartCharIndex { get; set; }
    public int EndCharIndex { get; set; }

    // Contenido
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }

    // Referencia a Qdrant
    public Guid QdrantPointId { get; set; }

    // Metadatos opcionales
    public string? PageNumber { get; set; }
    public string? SectionTitle { get; set; }
}
```

**Propósito:** Soportar chunking para RAG, donde cada documento se divide en fragmentos que se almacenan individualmente en Qdrant.

### 4. Proyecto de Tests Creado

**Archivo:** `tests/Babel.Domain.Tests/Babel.Domain.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Babel.Domain\Babel.Domain.csproj" />
  </ItemGroup>
</Project>
```

### 5. Tests Unitarios

**Archivos creados:**
- `tests/Babel.Domain.Tests/Entities/ProjectTests.cs` - 5 tests
- `tests/Babel.Domain.Tests/Entities/DocumentTests.cs` - 13 tests
- `tests/Babel.Domain.Tests/Entities/DocumentChunkTests.cs` - 9 tests

**Total:** 27 tests unitarios

## Decisiones de Diseño

### Chunking vs Documento Completo

**Decisión:** Implementar chunking para vectorización

**Razón:**
- Los modelos de embedding tienen límite de tokens (~8K)
- Documentos largos se truncan o pierden información
- Con chunking se puede encontrar el párrafo exacto relevante para RAG
- Mejor precisión en búsqueda semántica

### Versionado de Documentos

**Decisión:** Sin versionado

**Razón:** Simplicidad. Los archivos se sobrescriben al actualizar.

### Estrategia de Borrado

**Decisión:** Hard delete (borrado físico inmediato)

**Razón:** Usuario prefiere borrado inmediato sin papelera.

## Diagrama de Relaciones

```
┌──────────────┐       ┌──────────────────┐       ┌─────────────────┐
│   PROJECT    │ 1   N │     DOCUMENT     │ 1   N │ DOCUMENT_CHUNK  │
├──────────────┤───────├──────────────────┤───────├─────────────────┤
│ Id           │       │ Id               │       │ Id              │
│ Name         │       │ ProjectId (FK)   │       │ DocumentId (FK) │
│ Description  │       │ FileName         │       │ ChunkIndex      │
│ CreatedAt    │       │ FilePath (NAS)   │       │ Content         │
│ UpdatedAt    │       │ FileSizeBytes    │       │ QdrantPointId ──┼──→ Qdrant
└──────────────┘       │ Content          │       │ TokenCount      │
                       │ IsVectorized     │       │ PageNumber      │
                       │ Status           │       └─────────────────┘
                       └──────────────────┘
```

## Flujo de Vectorización con Chunks

1. **Subir documento** → Se guarda en NAS, se crea registro `Document`
2. **Extraer texto** → OCR o directo → Se guarda en `Document.Content`
3. **Chunking** → Se divide `Content` en fragmentos → Se crean registros `DocumentChunk`
4. **Vectorización** → Cada chunk se envía a Semantic Kernel → Se genera embedding
5. **Almacenar en Qdrant** → El embedding se guarda con payload:
   ```json
   {
     "documentId": "guid",
     "chunkId": "guid",
     "projectId": "guid",
     "chunkIndex": 0,
     "fileName": "documento.pdf"
   }
   ```
6. **Actualizar chunk** → Se guarda `QdrantPointId` en `DocumentChunk`

## Comandos Útiles

```bash
# Compilar solución
dotnet build

# Ejecutar tests
dotnet test --verbosity normal

# Resultado de tests
# Pruebas totales: 27
# Correcto: 27
```

## Próximos Pasos

1. **Crear migración EF** para los nuevos campos de Document y la tabla DocumentChunk
2. **Implementar servicio de chunking** que divida documentos en fragmentos
3. **Actualizar QdrantRepository** para manejar chunks en lugar de documentos completos
4. **Implementar Commands/Queries** con MediatR para Projects y Documents
5. **Crear FileStorageService** para almacenamiento en NAS

## Lecciones Aprendidas

1. **xUnit requiere using explícito**: Los archivos de test necesitan `using Xunit;` aunque se use ImplicitUsings
2. **FluentAssertions mejora legibilidad**: `project.Name.Should().Be(expectedName)` es más expresivo que Assert
3. **Chunking es estándar para RAG**: Dividir documentos en fragmentos permite mejor precisión en búsqueda semántica

## Estado Final del Proyecto

| Componente | Estado |
|------------|--------|
| Entidad Project mejorada | ✅ |
| Entidad Document mejorada | ✅ |
| Entidad DocumentChunk creada | ✅ |
| Proyecto de tests creado | ✅ |
| Tests unitarios (27) | ✅ Todos pasan |
| CLAUDE.md actualizado | ✅ |
| Solución compila | ✅ |
