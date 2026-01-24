# Sesión 2026-01-24 11:26 - Creación del Proyecto Base y Corrección de QdrantClient

## Resumen de la Sesión

Esta sesión cubrió la creación completa de la estructura del proyecto Babel usando Clean Architecture con .NET 10.0, y la corrección de un error crítico con la inicialización de QdrantClient.

## Cambios Implementados

### 1. Estructura del Proyecto Creada

**Solución .NET con 4 capas siguiendo Clean Architecture:**

```
Babel/
├── src/
│   ├── Babel.Domain/          # Capa de Dominio (sin dependencias)
│   ├── Babel.Application/     # Capa de Aplicación (depende de Domain)
│   ├── Babel.Infrastructure/  # Capa de Infraestructura (depende de Application)
│   └── Babel.API/             # Capa de Presentación (depende de Application e Infrastructure)
├── Babel.slnx                 # Archivo de solución
├── CLAUDE.md                  # Guía para Claude Code
└── README.md                  # Documentación del proyecto
```

### 2. Capa de Dominio (Babel.Domain)

**Archivos creados:**

- `Entities/BaseEntity.cs` - Entidad base con Id, CreatedAt, UpdatedAt
- `Entities/Project.cs` - Entidad de proyecto
- `Entities/Document.cs` - Entidad de documento
- `Enums/DocumentStatus.cs` - Estados del documento (Pending, Processing, Completed, Failed, PendingReview)
- `Enums/FileExtensionType.cs` - Tipos de archivo para enrutamiento de procesamiento

**Características:**
- Sin dependencias externas (pureza de dominio)
- Auto-generación de GUID para Ids
- Timestamps automáticos en CreatedAt/UpdatedAt
- Relaciones navegables entre Project y Document

### 3. Capa de Aplicación (Babel.Application)

**Archivos creados:**

- `Interfaces/IApplicationDbContext.cs` - Abstracción del DbContext
- `Interfaces/IHealthCheckService.cs` - Interfaz para health checks
- `DTOs/HealthCheckResult.cs` - DTO para respuestas de health check

**Paquetes instalados:**
- MediatR 14.0.0
- Microsoft.EntityFrameworkCore 10.0.2
- Microsoft.EntityFrameworkCore.Abstractions 10.0.2

### 4. Capa de Infraestructura (Babel.Infrastructure)

**Archivos creados:**

- `Data/BabelDbContext.cs` - DbContext con auto-configuración
- `Data/Configurations/ProjectConfiguration.cs` - Configuración EF para Project
- `Data/Configurations/DocumentConfiguration.cs` - Configuración EF para Document
- `Services/HealthCheckService.cs` - Implementación de health checks
- `DependencyInjection.cs` - Registro de servicios (DI)

**Paquetes instalados:**
- Microsoft.EntityFrameworkCore 10.0.2
- Microsoft.EntityFrameworkCore.SqlServer 10.0.2
- Microsoft.EntityFrameworkCore.Design 10.0.2
- Qdrant.Client 1.16.1
- Microsoft.Extensions.Diagnostics.HealthChecks 10.0.2
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.2

**Características clave:**
- DbContext con auto-discovery de configuraciones
- DocumentStatus almacenado como string (mejor legibilidad)
- QdrantClient registrado como Singleton
- HttpClient para Azure OCR con IHttpClientFactory
- Health checks para SQL Server, Qdrant y Azure OCR

### 5. Capa de API (Babel.API)

**Archivos creados:**

- `Controllers/HealthController.cs` - 4 endpoints de health check
- `Program.cs` - Entry point con configuración de servicios
- `appsettings.json` - Configuración base
- `appsettings.Development.json` - Configuración de desarrollo

**Paquetes instalados:**
- Swashbuckle.AspNetCore 6.5.0
- Microsoft.EntityFrameworkCore.Design 10.0.2
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.2

**Endpoints implementados:**
- `GET /api/health` - Health check completo (todos los servicios)
- `GET /api/health/sql-server` - Solo SQL Server
- `GET /api/health/qdrant` - Solo Qdrant
- `GET /api/health/azure-ocr` - Solo Azure Computer Vision OCR
- `GET /swagger` - Documentación Swagger UI

### 6. Migración de Entity Framework Core

**Migración creada:** `InitialCreate`

**Comando usado:**
```bash
dotnet ef migrations add InitialCreate \
  --project src/Babel.Infrastructure/Babel.Infrastructure.csproj \
  --startup-project src/Babel.API/Babel.API.csproj \
  --output-dir Data/Migrations
```

**Tablas creadas:**
- `Projects` (Id, Name, CreatedAt, UpdatedAt)
- `Documents` (Id, ProjectId, FileName, FilePath, FileExtension, Status, Content, RequiresOcr, OcrReviewed, ProcessedAt, CreatedAt, UpdatedAt)
- `__EFMigrationsHistory` (tracking de migraciones)

## Problema Encontrado y Solución

### Error: UriFormatException en QdrantClient

**Síntoma:**
```
System.UriFormatException: 'Invalid URI: The hostname could not be parsed.'
```

**Ubicación:** `src/Babel.Infrastructure/DependencyInjection.cs` línea 29

**Código problemático:**
```csharp
var qdrantEndpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
services.AddSingleton<QdrantClient>(sp =>
    new QdrantClient(qdrantEndpoint));  // ❌ Error
```

**Causa raíz:**
El constructor de `QdrantClient` versión 1.16.1 requiere un objeto `Uri`, no acepta strings directamente.

**Solución implementada:**
```csharp
var qdrantEndpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
Uri qdrantUri = new Uri(qdrantEndpoint);  // Convertir a Uri
services.AddSingleton<QdrantClient>(sp =>
    new QdrantClient(qdrantUri));  // ✓ Correcto
```

## Desafíos Técnicos Encontrados

### 1. Compatibilidad de .NET Framework

**Problema:** El proyecto inicialmente se creó con .NET 10.0, pero los paquetes de EF Core 9.0.1 solo soportaban .NET 8.0.

**Intentos de solución:**
1. Cambiar framework de net10.0 a net8.0
2. Intentar usar EF Core 9.0.1 con .NET 8.0
3. Instalar herramientas de EF Core

**Problema secundario:** El sistema solo tenía .NET 10.0 runtime instalado, no .NET 8.0.

**Solución final:**
- Revertir a .NET 10.0 en todos los proyectos
- Actualizar todos los paquetes a versión 10.0.2 (compatible con .NET 10.0)
- Instalar `dotnet-ef` tools versión 10.0.2

### 2. Paquetes NuGet Incompatibles

**Problema:** Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore versión 9.0.1 solo soporta .NET 9.0.

**Solución:**
- Usar versión 8.0.11 para proyectos con .NET 8.0
- Cambiar a versión 10.0.2 cuando se migró a .NET 10.0

### 3. OpenAPI vs Swagger

**Problema:** El template de .NET 10.0 usa `AddOpenApi()` y `MapOpenApi()` que no existen en .NET 8.0.

**Solución:**
- Cambiar de OpenAPI a Swashbuckle.AspNetCore
- Usar `AddSwaggerGen()`, `UseSwagger()` y `UseSwaggerUI()`
- Instalar paquete Swashbuckle.AspNetCore 6.5.0

### 4. Fuente NuGet Problemática

**Problema:** Fuente `CreameIA_shared` generaba errores 401 Unauthorized.

**Solución:**
```bash
dotnet nuget disable source CreameIA_shared
```

## Configuración Final

### appsettings.json



### Paquetes NuGet por Proyecto

**Babel.Domain:**
- .NET 10.0 (sin paquetes externos)

**Babel.Application:**
- MediatR 14.0.0
- Microsoft.EntityFrameworkCore 10.0.2
- Microsoft.EntityFrameworkCore.Abstractions 10.0.2

**Babel.Infrastructure:**
- Microsoft.EntityFrameworkCore 10.0.2
- Microsoft.EntityFrameworkCore.SqlServer 10.0.2
- Microsoft.EntityFrameworkCore.Design 10.0.2
- Qdrant.Client 1.16.1
- Microsoft.Extensions.Diagnostics.HealthChecks 10.0.2
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.2

**Babel.API:**
- Swashbuckle.AspNetCore 6.5.0
- Microsoft.EntityFrameworkCore.Design 10.0.2
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.2

## Próximos Pasos

### Inmediatos (para completar el setup)

1. **Iniciar servicios Docker:**
   ```bash
   docker-compose up -d
   ```

2. **Aplicar migraciones:**
   ```bash
   dotnet ef database update \
     --project src/Babel.Infrastructure/Babel.Infrastructure.csproj \
     --startup-project src/Babel.API/Babel.API.csproj
   ```

3. **Ejecutar aplicación:**
   ```bash
   dotnet run --project src/Babel.API/Babel.API.csproj
   ```

4. **Verificar health checks:**
   - https://localhost:5001/api/health
   - https://localhost:5001/swagger

### Siguientes Funcionalidades

**Fase 2: Gestión de Proyectos y Documentos**
- Commands/Queries de MediatR
- Controllers para Projects y Documents

**Fase 3: Procesamiento de Documentos**
- FileStorageService
- FileTypeDetectionService
- Hangfire para jobs en background

**Fase 4: Vectorización y Búsqueda**
- Colección de Qdrant
- VectorizationService con Semantic Kernel
- SearchService con búsqueda vectorial

**Fase 5: Chat RAG**
- ChatService con patrón RAG
- Semantic Kernel para LLMs
- Sistema de referencias a documentos

**Fase 6: UI con Blazor**
- Página principal con tarjetas de proyectos
- Detalle de proyecto con chat, lista de archivos y carga

## Comandos Útiles de Referencia

### Compilación y Build
```bash
# Compilar solución completa
dotnet build Babel.slnx

# Limpiar build artifacts
dotnet clean Babel.slnx

# Restaurar paquetes NuGet
dotnet restore Babel.slnx
```

### Entity Framework Core
```bash
# Crear migración
dotnet ef migrations add MigrationName \
  --project src/Babel.Infrastructure/Babel.Infrastructure.csproj \
  --startup-project src/Babel.API/Babel.API.csproj

# Aplicar migraciones
dotnet ef database update \
  --project src/Babel.Infrastructure/Babel.Infrastructure.csproj \
  --startup-project src/Babel.API/Babel.API.csproj

# Eliminar última migración
dotnet ef migrations remove \
  --project src/Babel.Infrastructure/Babel.Infrastructure.csproj \
  --startup-project src/Babel.API/Babel.API.csproj

# Eliminar base de datos (solo desarrollo)
dotnet ef database drop --force \
  --project src/Babel.Infrastructure/Babel.Infrastructure.csproj \
  --startup-project src/Babel.API/Babel.API.csproj
```

### Docker
```bash
# Iniciar todos los servicios
docker-compose up -d

# Detener servicios
docker-compose down

# Ver logs
docker-compose logs -f
docker logs babel-sqlserver
docker logs babel-qdrant

# Verificar servicios corriendo
docker ps

# Conectar a SQL Server
docker exec -it babel-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "<PASSWORD>"
```

### Health Checks
```bash
# Health check completo
curl https://localhost:5001/api/health

# Health checks individuales
curl https://localhost:5001/api/health/sql-server
curl https://localhost:5001/api/health/qdrant
curl https://localhost:5001/api/health/azure-ocr
```

## Lecciones Aprendidas

1. **Compatibilidad de frameworks:** Siempre verificar que todos los paquetes NuGet soporten el framework target antes de comenzar.

2. **QdrantClient API:** La versión 1.16.1 requiere objetos `Uri`, no strings. Siempre revisar documentación de paquetes externos.

3. **EF Core Tools:** La versión de `dotnet-ef` debe coincidir con la versión del runtime instalado.

4. **Clean Architecture:** Mantener las dependencias fluyendo hacia adentro (Domain ← Application ← Infrastructure ← API) previene problemas de acoplamiento.

5. **Health Checks:** Implementar health checks desde el inicio facilita enormemente el debugging de problemas de conectividad.

6. **Documentación continua:** Mantener CLAUDE.md actualizado con decisiones de diseño ahorra tiempo en futuras sesiones.

## Estado Final del Proyecto

✅ **Compilación exitosa:** Todo el código compila sin errores ni warnings
✅ **Migración creada:** Base de datos lista para ser creada
✅ **Health checks implementados:** Endpoints para verificar SQL Server, Qdrant y Azure OCR
✅ **Clean Architecture:** Separación clara de responsabilidades en 4 capas
✅ **DI configurado:** Todos los servicios registrados correctamente
✅ **Swagger habilitado:** Documentación automática de la API
✅ **Bug crítico resuelto:** QdrantClient inicializa correctamente

El proyecto está listo para aplicar migraciones y comenzar a ejecutarse. La estructura base está completa y lista para agregar las siguientes funcionalidades.
