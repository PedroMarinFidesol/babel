# Sesión 2026-01-26 16:28 - Corrección de Bugs: Qdrant gRPC y ChunkingService

## Resumen de la Sesión

Sesión enfocada en la corrección de dos bugs críticos:
1. Error de conexión a Qdrant relacionado con gRPC (usaba puerto REST en lugar de gRPC)
2. Bucle infinito en `ChunkingService` que bloqueaba los tests

También se agregaron servicios de vectorización y se mejoró el logging para diagnóstico.

## Cambios Implementados

### 1. Corrección de Conexión Qdrant (gRPC)

**Problema:** `Qdrant.Client` versión 1.16.1 usa gRPC por defecto (puerto 6334), pero el código configuraba el endpoint REST (puerto 6333).

**Archivos modificados:**

#### `src/Babel.Infrastructure/DependencyInjection.cs`
```csharp
// ANTES (incorrecto)
var qdrantEndpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
Uri qdrantUri = new Uri(qdrantEndpoint);
services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantUri));

// DESPUÉS (correcto)
var qdrantHost = configuration["Qdrant:Host"] ?? "localhost";
var qdrantGrpcPort = configuration.GetValue("Qdrant:GrpcPort", 6334);
services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantHost, qdrantGrpcPort));
```

#### `src/Babel.Infrastructure/Configuration/QdrantOptions.cs`
```csharp
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
```

#### `src/Babel.API/appsettings.json`
```json
"Qdrant": {
  "Host": "localhost",
  "GrpcPort": 6334,
  "HttpPort": 6333,
  "CollectionName": "babel_documents",
  "VectorSize": 768
}
```

### 2. Corrección de Bucle Infinito en ChunkingService

**Problema:** La lógica de avance en el algoritmo de chunking podía causar que `nextPosition <= currentPosition`, resultando en un bucle infinito.

**Archivo:** `src/Babel.Infrastructure/Services/ChunkingService.cs`

```csharp
// ANTES (podía causar bucle infinito)
currentPosition = endPosition - _options.ChunkOverlap;
if (currentPosition <= 0 || currentPosition >= normalizedText.Length - _options.MinChunkSize)
{
    currentPosition = endPosition;
}

// DESPUÉS (siempre avanza)
// Asegurar que endPosition siempre avanza al menos un carácter
if (endPosition <= currentPosition)
{
    endPosition = Math.Min(currentPosition + _options.MaxChunkSize, normalizedText.Length);
}

// ... procesar chunk ...

// Calcular siguiente posición considerando overlap
int nextPosition = endPosition - _options.ChunkOverlap;

// Asegurar que siempre avanzamos al menos un carácter
if (nextPosition <= currentPosition)
{
    nextPosition = endPosition;
}

currentPosition = nextPosition;
```

### 3. Mejora de Logging en InMemoryDocumentProcessingQueue

**Archivo:** `src/Babel.Infrastructure/Queues/InMemoryDocumentProcessingQueue.cs`

```csharp
public class InMemoryDocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly ILogger<InMemoryDocumentProcessingQueue> _logger;

    public InMemoryDocumentProcessingQueue(ILogger<InMemoryDocumentProcessingQueue> logger)
    {
        _logger = logger;
    }

    public string EnqueueTextExtraction(Guid documentId)
    {
        _logger.LogWarning(
            "InMemoryDocumentProcessingQueue: EnqueueTextExtraction llamado para {DocumentId}. " +
            "Hangfire no está configurado - el documento NO será procesado. " +
            "Configure ConnectionStrings:HangfireConnection o ConnectionStrings:DefaultConnection.",
            documentId);
        return Guid.NewGuid().ToString();
    }
    // ... similar para otros métodos
}
```

### 4. Actualización de ConfigurationValidator

**Archivo:** `src/Babel.Infrastructure/Configuration/ConfigurationValidator.cs`

```csharp
private void ValidateQdrant()
{
    var host = _configuration["Qdrant:Host"];
    if (string.IsNullOrWhiteSpace(host))
    {
        _warnings.Add("Qdrant:Host no está configurado. Se usará 'localhost'.");
    }

    var grpcPort = _configuration.GetValue<int>("Qdrant:GrpcPort");
    if (grpcPort <= 0 || grpcPort > 65535)
    {
        _warnings.Add("Qdrant:GrpcPort no está configurado o es inválido. Se usará 6334.");
    }
    // ...
}
```

### 5. Actualización de Tests

**Archivo:** `tests/Babel.Infrastructure.Tests/Services/QdrantVectorStoreServiceTests.cs`

```csharp
public QdrantVectorStoreServiceTests()
{
    _options = Options.Create(new QdrantOptions
    {
        Host = "localhost",
        GrpcPort = 6334,
        HttpPort = 6333,
        CollectionName = "test_collection",
        VectorSize = 768
    });
    // ...
}

[Fact]
public void Constructor_ShouldInitializeCorrectly()
{
    var client = new QdrantClient("localhost", 6334);
    // ...
}
```

## Problemas Encontrados y Soluciones

### Bug 1: UriFormatException en QdrantClient

**Síntoma:** `System.UriFormatException` o error de conexión gRPC al intentar conectar a Qdrant.

**Causa raíz:** `QdrantClient` v1.16.1 espera conexión gRPC (puerto 6334), pero se configuraba con URI HTTP del puerto REST (6333).

**Solución:** Usar el constructor `QdrantClient(host, port)` con el puerto gRPC explícito.

### Bug 2: Tests bloqueados indefinidamente

**Síntoma:** El test `ChunkText_LongText_ShouldReturnMultipleChunks` se bloqueaba y nunca terminaba.

**Causa raíz:** Cuando `FindBestBreakPoint` retornaba un valor cercano a `currentPosition`, el cálculo `endPosition - ChunkOverlap` podía ser menor o igual a `currentPosition`, causando que el algoritmo no avanzara.

**Solución:** Agregar verificación explícita para asegurar que siempre avanzamos al menos un carácter entre iteraciones.

## Desafíos Técnicos

### Configuración de Qdrant con diferentes puertos

Qdrant expone dos puertos:
- **6333:** REST API (HTTP)
- **6334:** gRPC

La biblioteca `Qdrant.Client` usa gRPC por defecto para mejor rendimiento. Es importante configurar ambos puertos correctamente según el uso:
- Operaciones programáticas: usar gRPC (6334)
- Dashboard y health checks: usar REST (6333)

### Algoritmo de Chunking con Overlap

El overlap entre chunks es necesario para mantener contexto en RAG, pero introduce complejidad:
- Si overlap >= tamaño del chunk, puede causar retroceso
- Si `FindBestBreakPoint` encuentra un punto de corte muy cercano al inicio, el avance puede ser mínimo

La solución fue agregar guardas explícitas para garantizar progreso.

## Configuración Final

### appsettings.json (Qdrant)
```json
"Qdrant": {
  "Host": "localhost",
  "GrpcPort": 6334,
  "HttpPort": 6333,
  "CollectionName": "babel_documents",
  "VectorSize": 768
}
```

### appsettings.local.json (actualizado)
```json
"Qdrant": {
  "Host": "localhost",
  "GrpcPort": 6334,
  "HttpPort": 6333,
  "CollectionName": "babel_documents_dev",
  "VectorSize": 1536
}
```

## Próximos Pasos

1. **Verificar jobs de Hangfire:** Reiniciar la aplicación y verificar que los jobs se encolan correctamente al subir archivos
2. **Probar vectorización end-to-end:** Subir un archivo .txt y verificar que se vectoriza en Qdrant
3. **Implementar extracción de PDF y Office:** Pendiente para completar Fase 6
4. **Implementar búsqueda semántica:** Query → embedding → búsqueda en Qdrant → resultados

## Comandos Útiles

```bash
# Verificar Qdrant está corriendo con gRPC
docker ps | grep qdrant
# Output: 0.0.0.0:6333-6334->6333-6334/tcp

# Verificar colecciones en Qdrant (REST)
curl http://localhost:6333/collections

# Ejecutar tests de ChunkingService
dotnet test tests/Babel.Infrastructure.Tests --filter "ChunkingService"

# Compilar solo Infrastructure
dotnet build src/Babel.Infrastructure/Babel.Infrastructure.csproj
```

## Lecciones Aprendidas

1. **Revisar la documentación de las bibliotecas:** `Qdrant.Client` cambió a gRPC por defecto en versiones recientes. Siempre verificar breaking changes.

2. **Tests de bucles deben tener timeout:** Los tests que involucran bucles deberían tener un timeout para detectar bucles infinitos rápidamente.

3. **Logging de fallbacks:** Cuando hay implementaciones de fallback (como `InMemoryDocumentProcessingQueue`), es crucial agregar warnings claros para facilitar el diagnóstico.

4. **Validar avance en algoritmos iterativos:** Siempre verificar que los algoritmos con bucles while avanzan en cada iteración.

## Estado Final del Proyecto

### Bugs Corregidos
- [x] Conexión a Qdrant vía gRPC
- [x] Bucle infinito en ChunkingService
- [x] Logging mejorado para diagnóstico de jobs

### Tests
- [x] 16/16 tests de ChunkingService pasan
- [x] Tests de QdrantVectorStoreService actualizados

### Commits de la Sesión
```
50e68bb fix: corregir bucle infinito en ChunkingService y mejorar logging de queue
2df83e1 feat: agregar servicios de vectorización y corregir conexión Qdrant gRPC
```

### Total de Tests del Proyecto
- Domain: 27 tests
- Application: 77 tests
- Infrastructure: 56 tests
- **Total: 160 tests**
