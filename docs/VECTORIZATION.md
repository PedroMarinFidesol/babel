# Vectorización en Babel - Guía Completa

## Tabla de Contenidos

1. [Introducción a la Vectorización](#1-introducción-a-la-vectorización)
2. [Conceptos Teóricos](#2-conceptos-teóricos)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Componentes del Sistema](#4-componentes-del-sistema)
5. [Flujo de Vectorización](#5-flujo-de-vectorización)
6. [Configuración](#6-configuración)
7. [Búsqueda Semántica y RAG](#7-búsqueda-semántica-y-rag)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. Introducción a la Vectorización

La vectorización es el proceso de convertir texto en representaciones numéricas (vectores) que capturan el significado semántico del contenido. En Babel, este proceso permite:

- **Búsqueda semántica**: Encontrar documentos por significado, no solo por palabras clave
- **Chat RAG**: Responder preguntas usando el contexto de los documentos del proyecto
- **Recuperación inteligente**: Identificar documentos relevantes incluso con terminología diferente

### Por qué Vectorización en lugar de Búsqueda de Texto

| Búsqueda Tradicional | Búsqueda Vectorial |
|---------------------|-------------------|
| Busca palabras exactas | Busca por significado |
| "automóvil" no encuentra "carro" | "automóvil" encuentra "carro", "vehículo", "coche" |
| Requiere keywords precisas | Entiende lenguaje natural |
| Rápida pero limitada | Más costosa pero precisa |

---

## 2. Conceptos Teóricos

### 2.1 Embeddings

Un **embedding** es un vector de números de punto flotante que representa el significado de un texto en un espacio multidimensional.

```
"El gato duerme" → [0.23, -0.45, 0.12, ..., 0.89]  // 1536 dimensiones
"El felino descansa" → [0.21, -0.43, 0.14, ..., 0.87]  // Vector similar
"La casa es grande" → [-0.56, 0.78, -0.34, ..., 0.12]  // Vector diferente
```

**Propiedades clave:**
- Textos con significado similar → vectores cercanos
- Textos con significado diferente → vectores lejanos
- La "distancia" se mide con similitud coseno (0 a 1)

### 2.2 Chunking (Fragmentación)

Los modelos de embedding tienen límites de tokens (típicamente 512-8192). Los documentos largos deben dividirse en **chunks** (fragmentos).

```
Documento (10,000 caracteres)
    ↓ Chunking
┌─────────────────┐
│ Chunk 0 (1000)  │ ← Embedding 0
├─────────────────┤
│ Chunk 1 (1000)  │ ← Embedding 1
├─────────────────┤
│ Chunk 2 (1000)  │ ← Embedding 2
└─────────────────┘
    ...
```

**Estrategia de Overlap:**
- Cada chunk incluye parte del chunk anterior
- Evita perder contexto en los límites
- Ejemplo: 200 caracteres de overlap

```
Chunk 0: [0-1000]
Chunk 1: [800-1800]    ← 200 chars de overlap con Chunk 0
Chunk 2: [1600-2600]   ← 200 chars de overlap con Chunk 1
```

### 2.3 Base de Datos Vectorial (Qdrant)

Qdrant almacena vectores y permite búsqueda por similitud:

```
┌─────────────────────────────────────────────────┐
│                    QDRANT                        │
├─────────────────────────────────────────────────┤
│ Collection: babel_documents                      │
│                                                  │
│ Point 1: {                                       │
│   id: "uuid-1",                                  │
│   vector: [0.23, -0.45, ...],  // 1536 dims     │
│   payload: {                                     │
│     document_id: "doc-uuid",                     │
│     project_id: "proj-uuid",                     │
│     chunk_index: 0,                              │
│     file_name: "documento.pdf"                   │
│   }                                              │
│ }                                                │
│                                                  │
│ Point 2: { ... }                                 │
│ Point 3: { ... }                                 │
└─────────────────────────────────────────────────┘
```

### 2.4 Similitud Coseno

Mide el ángulo entre dos vectores (0 = opuestos, 1 = idénticos):

```
           Vector A
          ↗
         /  θ = ángulo
        /
Origin ─────→ Vector B

Similitud = cos(θ)
- θ = 0°  → cos(0) = 1.0 (idénticos)
- θ = 90° → cos(90) = 0.0 (sin relación)
- θ = 180° → cos(180) = -1.0 (opuestos)
```

---

## 3. Arquitectura del Sistema

### 3.1 Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────┐
│                        BABEL SYSTEM                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   APPLICATION LAYER                      │    │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐    │    │
│  │  │IChunking    │ │IEmbedding   │ │IVectorStore     │    │    │
│  │  │Service      │ │Service      │ │Service          │    │    │
│  │  └──────┬──────┘ └──────┬──────┘ └───────┬─────────┘    │    │
│  └─────────┼───────────────┼────────────────┼──────────────┘    │
│            │               │                │                    │
│  ┌─────────┼───────────────┼────────────────┼──────────────┐    │
│  │         │    INFRASTRUCTURE LAYER        │              │    │
│  │         ▼               ▼                ▼              │    │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │    │
│  │  │Chunking     │ │Semantic     │ │Qdrant           │   │    │
│  │  │Service      │ │Kernel       │ │VectorStore      │   │    │
│  │  │             │ │Embedding    │ │Service          │   │    │
│  │  └─────────────┘ └──────┬──────┘ └───────┬─────────┘   │    │
│  │                         │                │              │    │
│  │         ┌───────────────┘                │              │    │
│  │         ▼                                ▼              │    │
│  │  ┌─────────────────┐              ┌─────────────┐      │    │
│  │  │ LLM Provider    │              │   QDRANT    │      │    │
│  │  │ ┌─────────────┐ │              │   (Docker)  │      │    │
│  │  │ │ Ollama      │ │              │   Port:6334 │      │    │
│  │  │ ├─────────────┤ │              └─────────────┘      │    │
│  │  │ │ OpenAI      │ │                                   │    │
│  │  │ ├─────────────┤ │                                   │    │
│  │  │ │ Gemini      │ │                                   │    │
│  │  │ └─────────────┘ │                                   │    │
│  │  └─────────────────┘                                   │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Modelo de Datos

```
┌──────────────┐       ┌──────────────────┐       ┌─────────────────┐
│   PROJECT    │ 1   N │     DOCUMENT     │ 1   N │ DOCUMENT_CHUNK  │
├──────────────┤───────├──────────────────┤───────├─────────────────┤
│ Id           │       │ Id               │       │ Id              │
│ Name         │       │ ProjectId (FK)   │       │ DocumentId (FK) │
│ Description  │       │ FileName         │       │ ChunkIndex      │
│ CreatedAt    │       │ Content          │       │ Content         │
│ UpdatedAt    │       │ IsVectorized     │       │ QdrantPointId ──┼──→ Qdrant
└──────────────┘       │ VectorizedAt     │       │ TokenCount      │
                       └──────────────────┘       │ StartCharIndex  │
                                                  │ EndCharIndex    │
                                                  └─────────────────┘
```

---

## 4. Componentes del Sistema

### 4.1 IChunkingService / ChunkingService

**Ubicación:**
- Interface: `src/Babel.Application/Interfaces/IChunkingService.cs`
- Implementación: `src/Babel.Infrastructure/Services/ChunkingService.cs`

**Propósito:** Divide texto largo en fragmentos manejables.

**Algoritmo:**
1. Normaliza whitespace **preservando párrafos** (unifica saltos de línea, reduce espacios múltiples)
2. Si texto ≤ MaxChunkSize: retorna chunk único
3. Si texto > MaxChunkSize:
   - Busca puntos de corte naturales (. ! ? seguidos de espacio)
   - Si no encuentra: busca espacio
   - Si no encuentra: corta en MaxChunkSize
4. Aplica overlap para mantener contexto
5. Filtra chunks menores a MinChunkSize (excepto último)

**Ejemplo:**
```csharp
var chunks = _chunkingService.ChunkText(document.Content, document.Id);
// Resultado: IReadOnlyList<ChunkResult>
// - ChunkResult.ChunkIndex: 0, 1, 2...
// - ChunkResult.Content: texto del chunk
// - ChunkResult.StartCharIndex: posición inicio
// - ChunkResult.EndCharIndex: posición fin
// - ChunkResult.EstimatedTokenCount: ~chars/4
```

### 4.2 IEmbeddingService / SemanticKernelEmbeddingService

**Ubicación:**
- Interface: `src/Babel.Application/Interfaces/IEmbeddingService.cs`
- Implementación: `src/Babel.Infrastructure/Services/SemanticKernelEmbeddingService.cs`

**Propósito:** Genera embeddings usando Microsoft Semantic Kernel.

**Proveedores soportados:**
- **Ollama** (local): nomic-embed-text, all-minilm
- **OpenAI**: text-embedding-ada-002, text-embedding-3-small
- **Google Gemini**: embedding-001

**Métodos:**
```csharp
// Embedding individual
Result<ReadOnlyMemory<float>> result = await _embeddingService
    .GenerateEmbeddingAsync("texto a vectorizar", cancellationToken);

// Embeddings en batch (más eficiente)
Result<IReadOnlyList<ReadOnlyMemory<float>>> results = await _embeddingService
    .GenerateEmbeddingsAsync(textos, cancellationToken);

// Dimensión del vector
int dims = _embeddingService.GetVectorDimension(); // 1536, 768, etc.
```

### 4.3 IVectorStoreService / QdrantVectorStoreService

**Ubicación:**
- Interface: `src/Babel.Application/Interfaces/IVectorStoreService.cs`
- Implementación: `src/Babel.Infrastructure/Services/QdrantVectorStoreService.cs`

**Propósito:** Operaciones CRUD en Qdrant.

**Métodos:**
```csharp
// Insertar chunks en batch
await _vectorStoreService.UpsertChunksAsync(new List<(Guid, ReadOnlyMemory<float>, ChunkPayload)>
{
    (pointId1, embedding1, payload1),
    (pointId2, embedding2, payload2)
});

// Eliminar por documento
await _vectorStoreService.DeleteByDocumentIdAsync(documentId);

// Eliminar por proyecto
await _vectorStoreService.DeleteByProjectIdAsync(projectId);

// Búsqueda semántica
var results = await _vectorStoreService.SearchAsync(
    queryVector,      // embedding de la consulta
    projectId,        // filtrar por proyecto
    topK: 5,          // máximo 5 resultados
    minScore: 0.7f    // similitud mínima 70%
);
```

### 4.4 DocumentVectorizationJob

**Ubicación:** `src/Babel.Infrastructure/Jobs/DocumentVectorizationJob.cs`

**Propósito:** Job de Hangfire que orquesta la vectorización completa.

**Configuración:**
```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
[JobDisplayName("Vectorizar documento: {0}")]
```

**Proceso:**
1. Obtiene documento con chunks existentes
2. Valida: Content no vacío, Status == Completed
3. Si re-vectorización: elimina chunks anteriores
4. Divide en chunks con ChunkingService
5. Genera embeddings en batch
6. Guarda en Qdrant primero
7. Guarda documento + chunks en SQL Server (**Transactions + Command-Based**):
   - Elimina chunks antiguos con `ExecuteDeleteAsync`
   - Inserta nuevos con `AddRangeAsync`
   - Actualiza estado del documento con `ExecuteUpdateAsync`

---

## 5. Flujo de Vectorización

### 5.1 Diagrama de Secuencia

```
┌──────────┐    ┌────────────┐    ┌──────────┐    ┌──────────┐    ┌────────┐    ┌────────┐
│  Upload  │    │  Hangfire  │    │ Chunking │    │Embedding │    │ Qdrant │    │SQL Srv │
│Controller│    │    Job     │    │ Service  │    │ Service  │    │        │    │        │
└────┬─────┘    └─────┬──────┘    └────┬─────┘    └────┬─────┘    └───┬────┘    └───┬────┘
     │                │                │               │              │             │
     │ Upload Doc     │                │               │              │             │
     ├───────────────►│                │               │              │             │
     │                │ Enqueue Job    │               │              │             │
     │                ├───────────────►│               │              │             │
     │                │                │               │              │             │
     │                │ Get Document   │               │              │             │
     │                ├────────────────┼───────────────┼──────────────┼────────────►│
     │                │◄───────────────┼───────────────┼──────────────┼─────────────┤
     │                │                │               │              │             │
     │                │ ChunkText()    │               │              │             │
     │                ├───────────────►│               │              │             │
     │                │ ChunkResult[]  │               │              │             │
     │                │◄───────────────┤               │              │             │
     │                │                │               │              │             │
     │                │ GenerateEmbeddingsAsync()      │              │             │
     │                ├────────────────┼──────────────►│              │             │
     │                │ float[][]      │               │              │             │
     │                │◄───────────────┼───────────────┤              │             │
     │                │                │               │              │             │
     │                │ UpsertChunksAsync()            │              │             │
     │                ├────────────────┼───────────────┼─────────────►│             │
     │                │ OK             │               │              │             │
     │                │◄───────────────┼───────────────┼──────────────┤             │
     │                │                │               │              │             │
     │                │ SaveChangesAsync() (Document + Chunks)        │             │
     │                ├────────────────┼───────────────┼──────────────┼────────────►│
     │                │ OK             │               │              │             │
     │                │◄───────────────┼───────────────┼──────────────┼─────────────┤
     │                │                │               │              │             │
```

### 5.2 Estados del Documento

```
┌─────────┐     Upload      ┌────────────┐    Text Extract    ┌───────────┐
│ Pending │────────────────►│ Processing │──────────────────►│ Completed │
└─────────┘                 └────────────┘                    └─────┬─────┘
                                  │                                 │
                                  │ Error                           │ Vectorization Job
                                  ▼                                 ▼
                            ┌──────────┐                    ┌─────────────────┐
                            │  Failed  │                    │ IsVectorized=T  │
                            └──────────┘                    │ VectorizedAt=Now│
                                                            └─────────────────┘
```

---

## 6. Configuración

### 6.1 appsettings.json

```json
{
  "Qdrant": {
    "Host": "localhost",
    "GrpcPort": 6334,
    "HttpPort": 6333,
    "CollectionName": "babel_documents",
    "VectorSize": 1536
  },

  "Chunking": {
    "MaxChunkSize": 1000,
    "ChunkOverlap": 200,
    "MinChunkSize": 100
  },

  "SemanticKernel": {
    "Provider": "OpenAI",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}
```

### 6.2 Opciones de Chunking

| Parámetro | Rango | Default | Descripción |
|-----------|-------|---------|-------------|
| MaxChunkSize | 100-10000 | 1000 | Tamaño máximo de chunk en caracteres |
| ChunkOverlap | 0-1000 | 200 | Caracteres de overlap entre chunks |
| MinChunkSize | 10-500 | 100 | Chunks menores se descartan |

### 6.3 Opciones de Qdrant

| Parámetro | Default | Descripción |
|-----------|---------|-------------|
| Host | localhost | Hostname de Qdrant |
| GrpcPort | 6334 | Puerto gRPC (operaciones) |
| HttpPort | 6333 | Puerto HTTP (dashboard) |
| CollectionName | babel_documents | Nombre de la colección |
| VectorSize | 1536 | Dimensiones del vector |

### 6.4 Dimensiones por Modelo

| Modelo | Proveedor | Dimensiones |
|--------|-----------|-------------|
| text-embedding-ada-002 | OpenAI | 1536 |
| text-embedding-3-small | OpenAI | 1536 |
| text-embedding-3-large | OpenAI | 3072 |
| nomic-embed-text | Ollama | 768 |
| all-minilm | Ollama | 384 |
| embedding-001 | Google | 768 |

---

## 7. Búsqueda Semántica y RAG

### 7.1 Flujo de Búsqueda

```
Pregunta del Usuario
        │
        ▼
┌───────────────────┐
│ Generar Embedding │  ← EmbeddingService
│ de la pregunta    │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ Buscar en Qdrant  │  ← VectorStoreService.SearchAsync()
│ topK=5, score>0.7 │     Filtrado por ProjectId
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ Obtener Chunks    │  ← SQL Server (DocumentChunk.Content)
│ de SQL Server     │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ Construir Prompt  │
│ con Contexto      │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│ Enviar a LLM      │  ← Semantic Kernel Chat
└─────────┬─────────┘
          │
          ▼
    Respuesta + Referencias
```

### 7.2 Ejemplo de Prompt RAG

```
SYSTEM: Eres un asistente que responde preguntas basándote en los documentos proporcionados.
Usa SOLO la información de los documentos. Si no encuentras la respuesta, dilo claramente.
Al final, lista los documentos que usaste.

CONTEXTO:
---
[Chunk 1 - documento.pdf, chunk 0]
El contrato establece que el plazo de entrega es de 30 días hábiles...

[Chunk 2 - especificaciones.docx, chunk 3]
Los requisitos técnicos incluyen compatibilidad con Windows 10 y...
---

USER: ¿Cuál es el plazo de entrega del contrato?
```

---

## 8. Troubleshooting

### 8.1 Error: Dimensiones no coinciden

**Síntoma:** Error al insertar en Qdrant sobre dimensiones incorrectas.

**Causa:** El modelo de embedding genera vectores de tamaño diferente al configurado.

**Solución:**
1. Verificar `VectorSize` en appsettings.json
2. Debe coincidir con el modelo (ej: OpenAI ada-002 = 1536)
3. Recrear la colección si cambió el tamaño

### 8.2 Error: Colección no existe

**Síntoma:** Error "Collection not found" al vectorizar.

**Causa:** QdrantInitializationService no se ejecutó o falló.

**Solución:**
1. Verificar que Qdrant esté corriendo: `docker ps`
2. Verificar logs de inicialización
3. Crear manualmente: `POST http://localhost:6333/collections/babel_documents`

### 8.3 Error: Timeout generando embeddings

**Síntoma:** Timeout al llamar al servicio de embeddings.

**Causa:** Modelo muy grande o muchos chunks.

**Solución:**
1. Reducir batch size
2. Usar modelo más pequeño
3. Verificar conectividad con proveedor

### 8.4 Error: DbUpdateConcurrencyException

**Síntoma:** Error al marcar documento como vectorizado.

**Causa:** Conflicto entre ExecuteUpdateAsync y tracking de EF.

**Solución:** Usar estrategia **Command-Based** con Transacciones explícitas.
En lugar de depender del Change Tracker de EF Core, usar comandos directos:

```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync();
// ... operaciones de borrado/inserción ...
await _dbContext.Documents
    .Where(d => d.Id == documentId)
    .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsVectorized, true)...);
await transaction.CommitAsync();
```

### 8.5 Chunks no aparecen en búsqueda

**Síntoma:** Vectorización exitosa pero búsqueda no retorna resultados.

**Verificar:**
1. Score mínimo (minScore): probar con 0.5
2. Filtro de proyecto: verificar projectId correcto
3. Contenido del chunk: verificar que no esté vacío
4. Dashboard Qdrant: http://localhost:6333/dashboard

---

## Apéndice: Estructura de Archivos

```
src/
├── Babel.Application/
│   └── Interfaces/
│       ├── IChunkingService.cs
│       ├── IEmbeddingService.cs
│       ├── IVectorStoreService.cs
│       └── IChatService.cs
│
├── Babel.Domain/
│   └── Entities/
│       └── DocumentChunk.cs
│
└── Babel.Infrastructure/
    ├── Configuration/
    │   ├── ChunkingOptions.cs
    │   └── QdrantOptions.cs
    │
    ├── Services/
    │   ├── ChunkingService.cs
    │   ├── SemanticKernelEmbeddingService.cs
    │   ├── QdrantVectorStoreService.cs
    │   └── QdrantInitializationService.cs
    │
    └── Jobs/
        └── DocumentVectorizationJob.cs
```
