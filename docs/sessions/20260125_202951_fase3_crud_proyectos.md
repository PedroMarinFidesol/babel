# Sesión 2026-01-25 20:29 - Fase 3: CRUD Completo de Proyectos

## Resumen de la Sesión

Se completó la **Fase 3: Gestión de Proyectos (CRUD)** del plan de desarrollo. Se implementaron los endpoints REST faltantes, la query de búsqueda de proyectos y 30 tests unitarios para los handlers de Commands y Queries.

## Cambios Implementados

### 1. SearchProjectsQuery y Handler

**src/Babel.Application/Projects/Queries/SearchProjectsQuery.cs**
```csharp
public sealed record SearchProjectsQuery(string SearchTerm) : IQuery<List<ProjectDto>>;
```

**src/Babel.Application/Projects/Queries/SearchProjectsQueryHandler.cs**
```csharp
public sealed class SearchProjectsQueryHandler : IQueryHandler<SearchProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;

    public async Task<Result<List<ProjectDto>>> Handle(
        SearchProjectsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return Result.Success(new List<ProjectDto>());
        }

        var projects = await _projectRepository.SearchByNameAsync(
            request.SearchTerm,
            cancellationToken);
        // ... mapeo a DTOs
    }
}
```

### 2. ProjectsController con Endpoints REST

**src/Babel.API/Controllers/ProjectsController.cs**
```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken cancellationToken)

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
}
```

### 3. Contratos de Request

**src/Babel.API/Contracts/CreateProjectRequest.cs**
```csharp
public sealed record CreateProjectRequest(string Name, string? Description);
```

**src/Babel.API/Contracts/UpdateProjectRequest.cs**
```csharp
public sealed record UpdateProjectRequest(string Name, string? Description);
```

### 4. Corrección en ProjectRepository

**src/Babel.Infrastructure/Repositories/ProjectRepository.cs**
```csharp
public async Task<IReadOnlyList<Project>> SearchByNameAsync(
    string searchTerm,
    CancellationToken cancellationToken = default)
{
    return await DbSet
        .Include(p => p.Documents)  // <-- Agregado para conteo de documentos
        .AsNoTracking()
        .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%"))
        .OrderByDescending(p => p.UpdatedAt)
        .ToListAsync(cancellationToken);
}
```

### 5. Tests Unitarios (30 nuevos)

**tests/Babel.Application.Tests/Projects/Commands/**
- `CreateProjectCommandHandlerTests.cs` (5 tests)
- `UpdateProjectCommandHandlerTests.cs` (5 tests)
- `DeleteProjectCommandHandlerTests.cs` (3 tests)

**tests/Babel.Application.Tests/Projects/Queries/**
- `GetProjectsQueryHandlerTests.cs` (5 tests)
- `GetProjectByIdQueryHandlerTests.cs` (5 tests)
- `SearchProjectsQueryHandlerTests.cs` (7 tests)

## Problemas Encontrados y Soluciones

### Error: Property 'Message' not found on Error

**Problema:** El controlador usaba `result.Error.Message` pero el record Error tiene `Description`.

**Solución:** Cambiar todas las referencias de `.Error.Message` a `.Error.Description`.

```csharp
// Incorrecto
return Problem(result.Error.Message, statusCode: 500);

// Correcto
return Problem(result.Error.Description, statusCode: 500);
```

### SearchByNameAsync sin Include de Documents

**Problema:** El método no incluía los Documents, causando que el conteo de documentos fuera siempre 0.

**Solución:** Agregar `.Include(p => p.Documents)` antes de la consulta.

## Desafíos Técnicos

1. **Decisión de DTOs de Request vs Commands:** Se optó por crear DTOs de Request separados (`CreateProjectRequest`, `UpdateProjectRequest`) en la capa API para desacoplar la API de los Commands internos.

2. **Manejo de errores HTTP:** Se implementó un mapeo de errores de dominio a códigos HTTP apropiados:
   - `Project.NotFound` → 404 NotFound
   - `Project.DuplicateName` → 409 Conflict
   - `Validation.Error` → 400 BadRequest

## Configuración Final

No se requirieron cambios de configuración en esta sesión.

## Próximos Pasos

1. **Fase 4: Almacenamiento de Archivos (NAS)**
   - Crear `IStorageService` interface
   - Implementar `LocalFileStorageService`
   - Configurar límites y validaciones de archivos

2. **Fase 5: Subida de Documentos**
   - `UploadDocumentCommand` y handler
   - Detección de tipo de archivo
   - Integración con FileUpload.razor

## Comandos Útiles

```bash
# Compilar solución
dotnet build

# Ejecutar tests
dotnet test

# Ejecutar API
cd src/Babel.API && dotnet run
```

## Lecciones Aprendidas

1. **Verificar estructura de records:** Antes de usar propiedades de un record, verificar su definición exacta (Error tiene `Description`, no `Message`).

2. **Include en queries de búsqueda:** Si el DTO resultante necesita conteos de relaciones, asegurarse de incluirlas con `.Include()`.

3. **Usar NSubstitute para mocking:** El patrón Arrange/Act/Assert con NSubstitute es limpio y legible para tests de handlers.

## Estado Final del Proyecto

### Completado en Fase 3
- [x] SearchProjectsQuery y Handler
- [x] ProjectsController con 6 endpoints REST
- [x] DTOs de Request (CreateProjectRequest, UpdateProjectRequest)
- [x] 30 tests unitarios de handlers
- [x] Documentación actualizada (PLAN_DESARROLLO.md, CLAUDE.md)

### Estadísticas de Tests
- **Total:** 88 tests (27 domain + 61 application)
- **Nuevos:** 30 tests de handlers

### Endpoints API Disponibles
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/projects` | Listar proyectos |
| GET | `/api/projects/{id}` | Obtener por ID |
| GET | `/api/projects/search?term=x` | Buscar por nombre |
| POST | `/api/projects` | Crear proyecto |
| PUT | `/api/projects/{id}` | Actualizar proyecto |
| DELETE | `/api/projects/{id}` | Eliminar proyecto |
