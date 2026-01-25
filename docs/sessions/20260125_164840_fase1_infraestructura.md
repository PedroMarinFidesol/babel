# Sesión 2026-01-25 16:48 - Fase 1: Infraestructura y Base de Datos

## Resumen de la Sesión

Implementación completa de la Fase 1 del plan de desarrollo de Babel: configuración de infraestructura Docker, migraciones de Entity Framework Core, servicios de inicialización de Qdrant y validación de configuración.

## Cambios Implementados

### 1. Docker Compose

**Archivos creados:**
- `docker-compose.yml` - SQL Server 2022, Qdrant, Azure OCR
- `docker-compose.override.yml` - Configuración para desarrollo local
- `.env.example` - Variables de entorno de ejemplo
- `scripts/sql/01-init-database.sql` - Script de inicialización de BD

```yaml
# docker-compose.yml - Servicios principales
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports: ["1433:1433"]
  qdrant:
    image: qdrant/qdrant:latest
    ports: ["6333:6333", "6334:6334"]
  azure-ocr:
    image: mcr.microsoft.com/azure-cognitive-services/vision/read:3.2
    ports: ["5000:5000"]
```

### 2. Clases de Configuración (Options Pattern)

**Archivos creados en `src/Babel.Infrastructure/Configuration/`:**
- `QdrantOptions.cs` - Endpoint, CollectionName, VectorSize
- `FileStorageOptions.cs` - Provider, BasePath, MaxFileSizeBytes, AllowedExtensions
- `SemanticKernelOptions.cs` - DefaultProvider, Ollama, OpenAI, Gemini
- `AzureOcrOptions.cs` - Endpoint, ApiKey, UseLocalContainer
- `ChunkingOptions.cs` - MaxChunkSize, ChunkOverlap, MinChunkSize
- `ConfigurationValidator.cs` - Validación de configuración al inicio

### 3. Entity Framework Core

**Archivos modificados/creados:**
- `IApplicationDbContext.cs` - Añadido DbSet<DocumentChunk>
- `BabelDbContext.cs` - Añadido DocumentChunks, supresión de warning PendingModelChanges
- `BabelDbContextFactory.cs` - Factory para migraciones en tiempo de diseño
- `DocumentChunkConfiguration.cs` - Configuración EF para DocumentChunk
- `DocumentConfiguration.cs` - Actualizado con todos los campos
- `ProjectConfiguration.cs` - Añadido índice por nombre y Description

**Nueva migración:** `20260125_153723_InitialCreate`
- Tabla Projects con Description
- Tabla Documents con campos completos (FileSizeBytes, ContentHash, MimeType, FileType, IsVectorized, VectorizedAt)
- Tabla DocumentChunks con índices optimizados

### 4. Servicios de Infraestructura

**`QdrantInitializationService.cs`:**
```csharp
// Crea la colección de Qdrant automáticamente al inicio
public class QdrantInitializationService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Verificar si existe, crear si no
        await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams
        {
            Size = (ulong)vectorSize,
            Distance = Distance.Cosine
        });
        // Crear índices de payload para filtrado
        await _qdrantClient.CreatePayloadIndexAsync(collectionName, "projectId", ...);
    }
}
```

### 5. Actualización de appsettings

**`appsettings.json`:**
```json
{
  "ConnectionStrings": { "DefaultConnection": "", "HangfireConnection": "" },
  "Qdrant": { "Endpoint": "http://localhost:6333", "CollectionName": "babel_documents", "VectorSize": 1536 },
  "FileStorage": { "Provider": "Local", "BasePath": "./uploads", "MaxFileSizeBytes": 104857600 },
  "SemanticKernel": { "DefaultProvider": "Ollama", ... },
  "Hangfire": { "DashboardPath": "/hangfire", "WorkerCount": 2 },
  "Chunking": { "MaxChunkSize": 1000, "ChunkOverlap": 200, "MinChunkSize": 100 }
}
```

### 6. Documentación

- `QUICKSTART.md` - Guía de inicio rápido
- `docs/PLAN_DESARROLLO.md` - Plan de desarrollo completo con 11 fases

### 7. Corrección de Skill end-session

- Eliminadas operaciones git automáticas (creación de ramas, commit/push)
- El usuario debe hacer commit/push manualmente

## Problemas Encontrados y Soluciones

### 1. Error PendingModelChangesWarning
**Problema:** EF Core mostraba warning de cambios pendientes incorrectamente en .NET 10

**Solución:** Suprimir el warning en OnConfiguring:
```csharp
optionsBuilder.ConfigureWarnings(warnings =>
    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
```

### 2. Error DbContext no resuelto en migraciones
**Problema:** `Unable to create a 'DbContext' of type 'BabelDbContext'`

**Solución:** Crear `BabelDbContextFactory` implementando `IDesignTimeDbContextFactory<BabelDbContext>`

### 3. Paquetes faltantes para Configuration
**Problema:** `SetBasePath` no encontrado

**Solución:** Añadir paquetes:
- `Microsoft.Extensions.Configuration.FileExtensions`
- `Microsoft.Extensions.Configuration.Json`

### 4. Conflictos en merge a main
**Problema:** Conflictos en appsettings.json y DependencyInjection.cs

**Solución:** Resolver manualmente tomando la versión más completa (nice-williams)

### 5. Problemas con worktrees y git
**Problema:** No se puede hacer checkout de main desde worktree

**Solución:** Hacer merge desde el repositorio principal (`C:\Users\pmarin\babel`)

## Desafíos Técnicos

1. **Migración de Data a Migrations:** EF Core movió el directorio de migraciones, requiriendo recrear la migración desde cero
2. **QdrantClient URI:** Versión 1.16.1 requiere objeto Uri, no string
3. **Worktrees de Git:** Requieren manejo especial para operaciones de merge

## Configuración Final

### Paquetes añadidos a Babel.Infrastructure:
- Microsoft.Extensions.Configuration.FileExtensions (10.0.2)
- Microsoft.Extensions.Configuration.Json (10.0.2)

### Estructura de carpetas creada:
```
src/Babel.Infrastructure/
├── Configuration/
│   ├── AzureOcrOptions.cs
│   ├── ChunkingOptions.cs
│   ├── ConfigurationValidator.cs
│   ├── FileStorageOptions.cs
│   ├── QdrantOptions.cs
│   └── SemanticKernelOptions.cs
├── Data/
│   ├── BabelDbContext.cs
│   ├── BabelDbContextFactory.cs
│   └── Configurations/
│       ├── DocumentChunkConfiguration.cs
│       ├── DocumentConfiguration.cs
│       └── ProjectConfiguration.cs
├── Migrations/
│   └── 20260125_153723_InitialCreate.cs
└── Services/
    └── QdrantInitializationService.cs
```

## Próximos Pasos

1. **Fase 2: CQRS con MediatR**
   - Instalar paquetes MediatR
   - Configurar pipeline behaviors
   - Implementar patrón Result<T>
   - Crear repositorios base

2. **Iniciar servicios Docker:**
   ```bash
   docker-compose up -d sqlserver qdrant
   dotnet ef database update --project src/Babel.Infrastructure --startup-project src/Babel.API
   ```

## Comandos Útiles

```bash
# Iniciar servicios
docker-compose up -d sqlserver qdrant

# Aplicar migraciones
dotnet ef database update --project src/Babel.Infrastructure --startup-project src/Babel.API

# Verificar Qdrant
curl http://localhost:6333/collections

# Compilar y testear
dotnet build
dotnet test
```

## Lecciones Aprendidas

1. **Worktrees requieren cuidado:** Las operaciones de merge deben hacerse desde el repositorio principal
2. **EF Core .NET 10:** Puede mostrar warnings falsos de cambios pendientes
3. **IDesignTimeDbContextFactory:** Es necesario para migraciones cuando el DbContext no se puede resolver automáticamente
4. **Options Pattern:** Mejor práctica para configuración tipada en .NET

## Estado Final del Proyecto

### Completado en esta sesión:
- [x] Docker Compose con SQL Server, Qdrant, Azure OCR
- [x] Clases de configuración (Options Pattern)
- [x] QdrantInitializationService
- [x] ConfigurationValidator
- [x] BabelDbContextFactory
- [x] Migración EF con todas las entidades
- [x] Documentación (QUICKSTART.md, PLAN_DESARROLLO.md)
- [x] Corrección de skill end-session

### Pendiente:
- [ ] Iniciar servicios Docker y aplicar migraciones (requiere Docker corriendo)
- [ ] Fase 2: MediatR y CQRS
