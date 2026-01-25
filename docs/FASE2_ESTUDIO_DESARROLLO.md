# Estudio de Desarrollo - Fase 2: CQRS con MediatR

## Resumen Ejecutivo

**Objetivo:** Establecer la arquitectura base para Commands y Queries usando MediatR, implementar el patrón Result para manejo de errores, y crear los repositorios base.

**Estado Actual:**
- ✅ MediatR 14.0.0 ya instalado en Babel.Application
- ✅ DTOs existentes (ProjectDto, DocumentDto, ChatMessageDto)
- ✅ IApplicationDbContext definido
- ❌ FluentValidation NO instalado
- ❌ Sin Commands/Queries implementados
- ❌ Sin repositorios

---

## Análisis de Dependencias

### Paquetes a Instalar

| Paquete | Proyecto | Versión | Propósito |
|---------|----------|---------|-----------|
| FluentValidation | Babel.Application | 11.11.0 | Validación de Commands |
| FluentValidation.DependencyInjectionExtensions | Babel.Application | 11.11.0 | Auto-registro de validators |
| MediatR | Babel.Infrastructure | 14.0.0 | Ya está, verificar |

### Dependencias Entre Componentes

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Babel.Application                            │
├─────────────────────────────────────────────────────────────────────┤
│  Commands/                     Queries/                              │
│  ├── CreateProjectCommand      ├── GetProjectsQuery                  │
│  ├── UpdateProjectCommand      ├── GetProjectByIdQuery               │
│  └── DeleteProjectCommand      └── SearchProjectsQuery               │
│                                                                      │
│  Common/                       Behaviors/                            │
│  ├── Result<T>                 ├── ValidationBehavior                │
│  ├── Error                     ├── LoggingBehavior                   │
│  ├── ICommand<TResult>         └── ExceptionHandlingBehavior         │
│  └── IQuery<TResult>                                                 │
│                                                                      │
│  Interfaces/                                                         │
│  ├── IProjectRepository                                              │
│  ├── IDocumentRepository                                             │
│  └── IUnitOfWork                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Babel.Infrastructure                          │
├─────────────────────────────────────────────────────────────────────┤
│  Repositories/                                                       │
│  ├── ProjectRepository : IProjectRepository                          │
│  ├── DocumentRepository : IDocumentRepository                        │
│  └── UnitOfWork : IUnitOfWork                                        │
│                                                                      │
│  DependencyInjection.cs                                              │
│  └── Registrar MediatR handlers, validators, repositories            │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Plan de Implementación Detallado

### SUBTAREA 2.1: Patrón Result y Errores de Dominio
**Archivos a crear:**

```
Babel.Application/
└── Common/
    ├── Result.cs              # Result<T> y Result (sin valor)
    ├── Error.cs               # Record para errores tipados
    └── DomainErrors.cs        # Errores comunes del dominio
```

#### Result.cs
```csharp
namespace Babel.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
```

#### Error.cs
```csharp
namespace Babel.Application.Common;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "El valor proporcionado es nulo.");
}
```

#### DomainErrors.cs
```csharp
namespace Babel.Application.Common;

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

        public static readonly Error NameTooLong = new(
            "Project.NameTooLong",
            "El nombre del proyecto excede los 200 caracteres.");
    }

    public static class Document
    {
        public static readonly Error NotFound = new(
            "Document.NotFound",
            "El documento no fue encontrado.");

        public static readonly Error ProjectNotFound = new(
            "Document.ProjectNotFound",
            "El proyecto asociado al documento no existe.");

        public static readonly Error FileTooLarge = new(
            "Document.FileTooLarge",
            "El archivo excede el tamaño máximo permitido.");

        public static readonly Error UnsupportedFileType = new(
            "Document.UnsupportedFileType",
            "El tipo de archivo no está soportado.");
    }
}
```

---

### SUBTAREA 2.2: Interfaces Base de CQRS
**Archivos a crear:**

```
Babel.Application/
└── Common/
    ├── ICommand.cs            # Interface marcadora para Commands
    └── IQuery.cs              # Interface marcadora para Queries
```

#### ICommand.cs
```csharp
using MediatR;

namespace Babel.Application.Common;

/// <summary>
/// Marcador para commands que modifican estado.
/// Todos los commands retornan Result o Result<T>.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
```

#### IQuery.cs
```csharp
using MediatR;

namespace Babel.Application.Common;

/// <summary>
/// Marcador para queries que solo leen datos.
/// Todos los queries retornan Result<T>.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
```

---

### SUBTAREA 2.3: Pipeline Behaviors
**Archivos a crear:**

```
Babel.Application/
└── Behaviors/
    ├── ValidationBehavior.cs
    ├── LoggingBehavior.cs
    └── ExceptionHandlingBehavior.cs
```

#### ValidationBehavior.cs
```csharp
using FluentValidation;
using MediatR;

namespace Babel.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

#### LoggingBehavior.cs
```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Babel.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Procesando {RequestName}",
            requestName);

        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        _logger.LogInformation(
            "Completado {RequestName} en {ElapsedMilliseconds}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

#### ExceptionHandlingBehavior.cs
```csharp
using Babel.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Babel.Application.Behaviors;

public sealed class ExceptionHandlingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;

    public ExceptionHandlingBehavior(
        ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogError(
                ex,
                "Error procesando {RequestName}: {Message}",
                requestName,
                ex.Message);

            // Retornar Result.Failure con el error
            var error = new Error("UnhandledException", ex.Message);

            // Usar reflexión para crear Result<T>.Failure
            if (typeof(TResponse).IsGenericType)
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                    .MakeGenericMethod(resultType);

                return (TResponse)failureMethod.Invoke(null, [error])!;
            }

            return (TResponse)(object)Result.Failure(error);
        }
    }
}
```

---

### SUBTAREA 2.4: Interfaces de Repositorios
**Archivos a crear:**

```
Babel.Application/
└── Interfaces/
    ├── IRepository.cs         # Repositorio genérico base
    ├── IUnitOfWork.cs         # Unit of Work
    ├── IProjectRepository.cs  # Repositorio específico de Project
    └── IDocumentRepository.cs # Repositorio específico de Document
```

#### IRepository.cs
```csharp
using Babel.Domain.Entities;
using System.Linq.Expressions;

namespace Babel.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(T entity);

    void Update(T entity);

    void Remove(T entity);
}
```

#### IUnitOfWork.cs
```csharp
namespace Babel.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    IDocumentRepository Documents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

#### IProjectRepository.cs
```csharp
using Babel.Domain.Entities;

namespace Babel.Application.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByIdWithDocumentsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetAllWithDocumentCountAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid excludeId,
        CancellationToken cancellationToken = default);
}
```

#### IDocumentRepository.cs
```csharp
using Babel.Domain.Entities;
using Babel.Domain.Enums;

namespace Babel.Application.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByIdWithChunksAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByHashAsync(
        string contentHash,
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<int> CountByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(
        Guid projectId,
        DocumentStatus status,
        CancellationToken cancellationToken = default);
}
```

---

### SUBTAREA 2.5: Implementación de Repositorios
**Archivos a crear:**

```
Babel.Infrastructure/
└── Repositories/
    ├── RepositoryBase.cs      # Implementación genérica
    ├── ProjectRepository.cs
    ├── DocumentRepository.cs
    └── UnitOfWork.cs
```

#### RepositoryBase.cs
```csharp
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Babel.Infrastructure.Repositories;

public abstract class RepositoryBase<T> : IRepository<T> where T : BaseEntity
{
    protected readonly BabelDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected RepositoryBase(BabelDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual void Add(T entity)
    {
        DbSet.Add(entity);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
```

#### ProjectRepository.cs
```csharp
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Babel.Infrastructure.Repositories;

public sealed class ProjectRepository : RepositoryBase<Project>, IProjectRepository
{
    public ProjectRepository(BabelDbContext context) : base(context)
    {
    }

    public async Task<Project?> GetByIdWithDocumentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllWithDocumentCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Documents)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid excludeId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Name == name && p.Id != excludeId, cancellationToken);
    }
}
```

#### DocumentRepository.cs
```csharp
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Babel.Infrastructure.Repositories;

public sealed class DocumentRepository : RepositoryBase<Document>, IDocumentRepository
{
    public DocumentRepository(BabelDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetByIdWithChunksAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByHashAsync(
        string contentHash,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(d => d.ContentHash == contentHash && d.ProjectId == projectId,
                cancellationToken);
    }

    public async Task<int> CountByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(d => d.ProjectId == projectId, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(
        Guid projectId,
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(d => d.ProjectId == projectId && d.Status == status,
                cancellationToken);
    }
}
```

#### UnitOfWork.cs
```csharp
using Babel.Application.Interfaces;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Babel.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BabelDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProjectRepository? _projects;
    private IDocumentRepository? _documents;

    public UnitOfWork(BabelDbContext context)
    {
        _context = context;
    }

    public IProjectRepository Projects =>
        _projects ??= new ProjectRepository(_context);

    public IDocumentRepository Documents =>
        _documents ??= new DocumentRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

---

### SUBTAREA 2.6: Registro de Servicios
**Archivos a modificar:**

```
Babel.Application/
└── DependencyInjection.cs     # NUEVO - Registro de MediatR y behaviors

Babel.Infrastructure/
└── DependencyInjection.cs     # MODIFICAR - Agregar repositorios
```

#### Babel.Application/DependencyInjection.cs (NUEVO)
```csharp
using Babel.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Babel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors (orden importa)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

#### Modificaciones a Babel.Infrastructure/DependencyInjection.cs
```csharp
// Agregar en AddInfrastructure():

// Repositorios
services.AddScoped<IProjectRepository, ProjectRepository>();
services.AddScoped<IDocumentRepository, DocumentRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## Estructura de Carpetas Final

```
Babel.Application/
├── Babel.Application.csproj        # + FluentValidation
├── DependencyInjection.cs          # NUEVO
├── Common/
│   ├── Result.cs                   # NUEVO
│   ├── Error.cs                    # NUEVO
│   ├── DomainErrors.cs             # NUEVO
│   ├── ICommand.cs                 # NUEVO
│   └── IQuery.cs                   # NUEVO
├── Behaviors/
│   ├── ValidationBehavior.cs       # NUEVO
│   ├── LoggingBehavior.cs          # NUEVO
│   └── ExceptionHandlingBehavior.cs # NUEVO
├── DTOs/                           # YA EXISTE
│   ├── ProjectDto.cs
│   ├── DocumentDto.cs
│   └── ...
└── Interfaces/
    ├── IApplicationDbContext.cs    # YA EXISTE
    ├── IHealthCheckService.cs      # YA EXISTE
    ├── IRepository.cs              # NUEVO
    ├── IUnitOfWork.cs              # NUEVO
    ├── IProjectRepository.cs       # NUEVO
    └── IDocumentRepository.cs      # NUEVO

Babel.Infrastructure/
├── DependencyInjection.cs          # MODIFICAR
└── Repositories/                   # NUEVO
    ├── RepositoryBase.cs
    ├── ProjectRepository.cs
    ├── DocumentRepository.cs
    └── UnitOfWork.cs
```

---

## Tests a Implementar

### Tests de Result<T>
```
Babel.Application.Tests/
└── Common/
    └── ResultTests.cs
```

**Casos de prueba:**
1. `Success_ShouldReturnIsSuccessTrue`
2. `Failure_ShouldReturnIsFailureTrueAndError`
3. `ImplicitConversion_ShouldWrapValueInSuccess`
4. `Value_WhenFailure_ShouldReturnDefault`

### Tests de Pipeline Behaviors
```
Babel.Application.Tests/
└── Behaviors/
    ├── ValidationBehaviorTests.cs
    └── LoggingBehaviorTests.cs
```

**Casos de prueba ValidationBehavior:**
1. `Handle_WithNoValidators_ShouldCallNext`
2. `Handle_WithValidRequest_ShouldCallNext`
3. `Handle_WithInvalidRequest_ShouldThrowValidationException`

### Tests de Repositorios
```
Babel.Infrastructure.Tests/
└── Repositories/
    ├── ProjectRepositoryTests.cs
    └── DocumentRepositoryTests.cs
```

**Casos de prueba ProjectRepository:**
1. `GetByIdAsync_ExistingProject_ShouldReturnProject`
2. `GetByIdAsync_NonExistingProject_ShouldReturnNull`
3. `GetAllWithDocumentCountAsync_ShouldIncludeDocuments`
4. `ExistsByNameAsync_ExistingName_ShouldReturnTrue`
5. `Add_ShouldTrackNewProject`

---

## Orden de Implementación Recomendado

```
PASO 1: Instalar paquetes NuGet
├── FluentValidation 11.11.0
└── FluentValidation.DependencyInjectionExtensions 11.11.0

PASO 2: Crear Common/ (sin dependencias)
├── Error.cs
├── Result.cs
├── DomainErrors.cs
├── ICommand.cs
└── IQuery.cs

PASO 3: Crear Behaviors/ (depende de Common)
├── LoggingBehavior.cs
├── ValidationBehavior.cs
└── ExceptionHandlingBehavior.cs

PASO 4: Crear Interfaces de Repositorios (depende de Domain)
├── IRepository.cs
├── IUnitOfWork.cs
├── IProjectRepository.cs
└── IDocumentRepository.cs

PASO 5: Implementar Repositorios (depende de Interfaces)
├── RepositoryBase.cs
├── ProjectRepository.cs
├── DocumentRepository.cs
└── UnitOfWork.cs

PASO 6: Registrar Servicios
├── Babel.Application/DependencyInjection.cs (nuevo)
└── Babel.Infrastructure/DependencyInjection.cs (modificar)

PASO 7: Actualizar Program.cs
├── Babel.API/Program.cs
└── Babel.WebUI/Program.cs

PASO 8: Crear Tests
├── ResultTests.cs
├── ValidationBehaviorTests.cs
└── ProjectRepositoryTests.cs
```

---

## Checklist de Entregables

### Código
- [ ] FluentValidation instalado en Babel.Application
- [ ] Carpeta Common/ con Result, Error, DomainErrors
- [ ] Interfaces ICommand/IQuery definidas
- [ ] Carpeta Behaviors/ con 3 behaviors
- [ ] Interfaces de repositorios en Babel.Application
- [ ] Implementaciones de repositorios en Babel.Infrastructure
- [ ] UnitOfWork implementado
- [ ] DependencyInjection.cs en Babel.Application
- [ ] DependencyInjection.cs actualizado en Babel.Infrastructure
- [ ] Program.cs actualizados (API y WebUI)

### Tests
- [ ] ResultTests (4+ tests)
- [ ] ValidationBehaviorTests (3+ tests)
- [ ] LoggingBehaviorTests (2+ tests)
- [ ] ProjectRepositoryTests (5+ tests)
- [ ] DocumentRepositoryTests (5+ tests)

### Documentación
- [ ] Actualizar CLAUDE.md con estado de Fase 2
- [ ] Actualizar PLAN_DESARROLLO.md con checkboxes completados

---

## Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| FluentValidation incompatible con .NET 10 | Baja | Alto | Usar versión 11.11.0 probada |
| MediatR pipeline order incorrecto | Media | Medio | Documentar orden: Log → Validate → Exception |
| EF Core tracking issues en repositorios | Baja | Medio | Usar AsNoTracking para queries |
| Unit of Work scope incorrecto | Media | Alto | Registrar como Scoped |

---

## Notas de Implementación

1. **Orden de Behaviors:** El orden en el pipeline es crítico:
   - LoggingBehavior primero (para registrar todas las requests)
   - ValidationBehavior segundo (para rechazar requests inválidas antes de procesar)
   - ExceptionHandlingBehavior tercero (para capturar excepciones no manejadas)

2. **Result vs Exceptions:** Usar Result para errores de negocio esperados, excepciones solo para errores técnicos inesperados.

3. **Repositorios y DbContext:** Los repositorios no hacen SaveChanges, eso lo hace UnitOfWork para mantener transaccionalidad.

4. **Validators:** Crear validators junto a los Commands en la Fase 3, no en esta fase.

---

*Documento creado: 2026-01-25*
*Para: Fase 2 - CQRS con MediatR*
