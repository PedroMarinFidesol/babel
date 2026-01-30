# Informe del Sistema de Vectorización de Babel

## 1. Resumen Ejecutivo

El sistema de vectorización de Babel convierte documentos de texto en representaciones vectoriales (embeddings) para habilitar búsqueda semántica y funcionalidades RAG (Retrieval Augmented Generation). El proceso se ejecuta de forma asíncrona mediante Hangfire después de la extracción de texto.

## 2. Arquitectura de Componentes

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          CAPA DE APLICACIÓN                             │
├─────────────────────────────────────────────────────────────────────────┤
│  Interfaces (Babel.Application/Interfaces/)                             │
│  ├── IChunkingService      → Divide texto en chunks                     │
│  ├── IEmbeddingService     → Genera embeddings vectoriales              │
│  └── IVectorStoreService   → Operaciones CRUD en Qdrant                 │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        CAPA DE INFRAESTRUCTURA                          │
├─────────────────────────────────────────────────────────────────────────┤
│  Jobs (Babel.Infrastructure/Jobs/)                                      │
│  └── DocumentVectorizationJob   → Orquesta todo el proceso              │
│                                                                         │
│  Services (Babel.Infrastructure/Services/)                              │
│  ├── ChunkingService            → Implementación de IChunkingService    │
│  ├── SemanticKernelEmbeddingService → Implementación de IEmbeddingService│
│  └── QdrantVectorStoreService   → Implementación de IVectorStoreService │
└─────────────────────────────────────────────────────────────────────────┘
```

## 3. Flujo de Estados de un Documento

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   PENDING    │────▶│  PROCESSING  │────▶│  COMPLETED   │────▶│  VECTORIZED  │
│              │     │              │     │              │     │              │
│ (Documento   │     │ (Extracción  │     │ (Texto       │     │ (Embeddings  │
│  subido)     │     │  de texto)   │     │  extraído)   │     │  en Qdrant)  │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
                                                │
                                                ▼
                          ┌─────────────────────────────────────────────┐
                          │      DocumentVectorizationJob               │
                          │  1. Obtener documento con chunks            │
                          │  2. Validar Status == Completed             │
                          │  3. ChunkingService.ChunkText()             │
                          │  4. EmbeddingService.GenerateEmbeddingsAsync│
                          │  5. QdrantVectorStoreService.UpsertChunks() │
                          │  6. MarkAsVectorizedAsync()                 │
                          └─────────────────────────────────────────────┘
```

### Estados del Documento (DocumentStatus)

| Estado | Descripción | IsVectorized |
|--------|-------------|--------------|
| `Pending` | Documento subido, esperando procesamiento | `false` |
| `Processing` | Extrayendo texto (DocumentProcessingJob) | `false` |
| `Completed` | Texto extraído, listo para vectorizar | `false` |
| `Completed` | Vectorización completada | `true` |
| `Failed` | Error en procesamiento | `false` |

## 4. Servicios Detallados

### 4.1 ChunkingService

**Ubicación:** `src/Babel.Infrastructure/Services/ChunkingService.cs`

**Propósito:** Divide el texto del documento en fragmentos más pequeños (chunks) para optimizar la búsqueda semántica.

**Algoritmo:**
1. Normaliza el texto (elimina espacios excesivos)
2. Si el texto cabe en un solo chunk, lo retorna completo
3. Si no, divide con overlap configurable
4. Busca puntos de corte naturales (fin de oración, espacio)
5. Estima tokens (~4 caracteres por token)

**Configuración:** `ChunkingOptions`
```json
"Chunking": {
  "MaxChunkSize": 1000,    // Máximo caracteres por chunk
  "ChunkOverlap": 200,     // Solapamiento entre chunks
  "MinChunkSize": 100      // Tamaño mínimo para procesar
}
```

**Salida:** `ChunkResult` record
- `ChunkIndex`: Posición del chunk (0, 1, 2...)
- `Content`: Texto del chunk
- `StartCharIndex` / `EndCharIndex`: Posiciones en texto original
- `EstimatedTokenCount`: Tokens estimados

### 4.2 SemanticKernelEmbeddingService

**Ubicación:** `src/Babel.Infrastructure/Services/SemanticKernelEmbeddingService.cs`

**Propósito:** Genera vectores de embedding usando Microsoft Semantic Kernel.

**Proveedores soportados:**
- **OpenAI:** `text-embedding-ada-002` (1536 dimensiones)
- **Ollama:** `nomic-embed-text` (768 dimensiones)

**Interfaz clave:** `ITextEmbeddingGenerationService` (de Semantic Kernel)

**Métodos:**
- `GenerateEmbeddingAsync(text)`: Un solo texto → Un vector
- `GenerateEmbeddingsAsync(texts)`: Batch de textos → Batch de vectores
- `GetVectorDimension()`: Retorna dimensión configurada en Qdrant

**Configuración:** `SemanticKernel` section
```json
"SemanticKernel": {
  "DefaultProvider": "OpenAI",
  "OpenAI": {
    "ApiKey": "sk-proj-...",
    "EmbeddingModel": "text-embedding-ada-002"
  },
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text"
  }
}
```

### 4.3 QdrantVectorStoreService

**Ubicación:** `src/Babel.Infrastructure/Services/QdrantVectorStoreService.cs`

**Propósito:** Almacena y gestiona vectores en Qdrant (base de datos vectorial).

**Operaciones:**
- `UpsertChunkAsync()`: Insertar/actualizar un chunk
- `UpsertChunksAsync()`: Insertar/actualizar batch (usado en vectorización)
- `DeleteByDocumentIdAsync()`: Eliminar todos los chunks de un documento
- `DeleteByProjectIdAsync()`: Eliminar todos los chunks de un proyecto

**Payload almacenado en Qdrant:**
```json
{
  "document_id": "guid",
  "project_id": "guid",
  "chunk_index": 0,
  "file_name": "documento.txt"
}
```

**Configuración:** `Qdrant` section
```json
"Qdrant": {
  "Host": "localhost",
  "GrpcPort": 6334,
  "RestPort": 6333,
  "CollectionName": "babel_documents",
  "VectorSize": 1536
}
```

### 4.4 DocumentVectorizationJob

**Ubicación:** `src/Babel.Infrastructure/Jobs/DocumentVectorizationJob.cs`

**Propósito:** Job de Hangfire que orquesta todo el proceso de vectorización.

**Atributos:**
- `[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]`
- `[JobDisplayName("Vectorizar documento: {0}")]`

**Flujo de ejecución:**

```
ProcessAsync(documentId)
       │
       ▼
┌──────────────────────────────────────┐
│ 1. Obtener documento con chunks      │
│    _documentRepository               │
│    .GetByIdWithChunksAsync()         │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 2. Validaciones                      │
│    - ¿Documento existe?              │
│    - ¿Tiene contenido?               │
│    - ¿Status == Completed?           │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 3. Si re-vectorización:              │
│    - Eliminar chunks de Qdrant       │
│    - Limpiar chunks de BD            │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 4. Dividir en chunks                 │
│    _chunkingService.ChunkText()      │
│    Resultado: List<ChunkResult>      │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 5. Generar embeddings (batch)        │
│    _embeddingService                 │
│    .GenerateEmbeddingsAsync()        │
│    Resultado: List<float[]>          │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 6. Crear DocumentChunks en BD        │
│    Para cada chunk:                  │
│    - Crear entidad DocumentChunk     │
│    - Asignar QdrantPointId           │
│    - Agregar a document.Chunks       │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 7. Guardar en Qdrant                 │
│    _vectorStoreService               │
│    .UpsertChunksAsync()              │
└──────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ 8. Marcar como vectorizado           │
│    _documentRepository               │
│    .MarkAsVectorizedAsync()          │
│    (Update directo para evitar       │
│     conflictos de concurrencia)      │
└──────────────────────────────────────┘
```

## 5. Modelo de Datos

### DocumentChunk (SQL Server)

```
┌─────────────────────────────────────┐
│         DOCUMENT_CHUNK              │
├─────────────────────────────────────┤
│ Id              Guid (PK)           │
│ DocumentId      Guid (FK → Document)│
│ ChunkIndex      int                 │
│ StartCharIndex  int                 │
│ EndCharIndex    int                 │
│ Content         string              │
│ TokenCount      int                 │
│ QdrantPointId   Guid ─────────────────────┐
│ PageNumber      string?             │     │
│ SectionTitle    string?             │     │
│ CreatedAt       DateTime            │     ▼
│ UpdatedAt       DateTime            │   Qdrant
└─────────────────────────────────────┘   Point
```

### Punto en Qdrant

```json
{
  "id": "uuid (QdrantPointId)",
  "vector": [0.123, -0.456, ...],  // 1536 dimensiones (ada-002)
  "payload": {
    "document_id": "guid",
    "project_id": "guid",
    "chunk_index": 0,
    "file_name": "documento.txt"
  }
}
```

## 6. Configuración Completa

### appsettings.json

```json
{
  "Qdrant": {
    "Host": "localhost",
    "GrpcPort": 6334,
    "RestPort": 6333,
    "CollectionName": "babel_documents",
    "VectorSize": 1536
  },
  "SemanticKernel": {
    "DefaultProvider": "OpenAI",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ChatModel": "llama2",
      "EmbeddingModel": "nomic-embed-text"
    },
    "OpenAI": {
      "ApiKey": "",
      "ChatModel": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  },
  "Chunking": {
    "MaxChunkSize": 1000,
    "ChunkOverlap": 200,
    "MinChunkSize": 100
  },
  "Hangfire": {
    "DashboardPath": "/hangfire",
    "WorkerCount": 1
  }
}
```

### appsettings.local.json (secretos - NO commitear)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BabelDb;..."
  },
  "SemanticKernel": {
    "DefaultProvider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-proj-..."
    }
  }
}
```

## 7. Dependencias de Paquetes NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.SemanticKernel` | 1.47.0+ | Framework de IA |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | 1.47.0+ | Conector OpenAI |
| `Qdrant.Client` | 1.16.1 | Cliente Qdrant |
| `Hangfire.Core` | 1.8.x | Jobs en background |
| `Hangfire.SqlServer` | 1.8.x | Storage SQL Server |
| `Hangfire.AspNetCore` | 1.8.x | Integración ASP.NET |

## 8. Manejo de Errores

El sistema utiliza el **patrón Result** para manejo funcional de errores:

```csharp
// Errores de dominio definidos
public static class DomainErrors
{
    public static class Vectorization
    {
        public static Error EmptyContent = new("Vectorization.EmptyContent", "...");
        public static Error ProviderNotConfigured = new("Vectorization.ProviderNotConfigured", "...");
        public static Error EmbeddingFailed = new("Vectorization.EmbeddingFailed", "...");
    }
}
```

**Reintentos automáticos:**
- 3 intentos máximo
- Delays: 60s, 300s (5min), 900s (15min)

## 9. Verificación del Sistema

### Dashboard de Hangfire
- URL: `https://localhost:5001/hangfire`
- Ver jobs en progreso, fallidos, completados

### Dashboard de Qdrant
- URL: `http://localhost:6333/dashboard`
- Ver colecciones y puntos almacenados

### Verificar documento vectorizado (SQL)
```sql
SELECT Id, FileName, IsVectorized, VectorizedAt
FROM Documents
WHERE ProjectId = 'guid'
```

### Verificar chunks en Qdrant (curl)
```bash
curl http://localhost:6333/collections/babel_documents/points/count
```

## 10. Problemas Conocidos y Soluciones

### Error: Proveedor de embeddings no configurado
**Causa:** Falta `SemanticKernel` section en appsettings o API key vacía.
**Solución:** Configurar `appsettings.local.json` con la API key de OpenAI.

### Error: DbUpdateConcurrencyException
**Causa:** Documento modificado entre lectura y escritura.
**Solución:** Usar `MarkAsVectorizedAsync()` que hace update directo sin tracking.

### Error: Qdrant connection refused
**Causa:** Puerto incorrecto (REST 6333 vs gRPC 6334).
**Solución:** Usar `GrpcPort: 6334` en configuración de Qdrant.

---

*Documento generado: 2026-01-30*
*Versión del sistema: Babel v1.0*
