# AGENTS.md

This file guides agentic coding assistants working in the Babel repository.

## Build/Test Commands

### Build Commands
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Babel.Domain/Babel.Domain.csproj

# Clean and rebuild
dotnet clean && dotnet build
```

### Test Commands
```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test tests/Babel.Domain.Tests

# Run single test with full name
dotnet test --filter "FullyQualifiedName~Constructor_ShouldInitializeWithDefaultValues"

# Run tests by class name
dotnet test --filter "FullyQualifiedName~ProjectTests"

# Run tests in verbose mode
dotnet test --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Commands
```bash
# Run API (with appsettings.local.json)
cd src/Babel.API
dotnet run

# Run WebUI (with appsettings.local.json)
cd src/Babel.WebUI
dotnet run
```

### Database Commands
```bash
# Create new migration
dotnet ef migrations add MigrationName --project Babel.Infrastructure --startup-project Babel.API

# Apply migrations
dotnet ef database update --project Babel.Infrastructure --startup-project Babel.API

# Generate SQL script
dotnet ef migrations script --project Babel.Infrastructure --startup-project Babel.API
```

### Docker Commands
```bash
# Start all services (SQL Server, Qdrant, Azure OCR)
docker-compose up -d

# View logs
docker-compose logs -f
docker-compose logs -f sqlserver
docker-compose logs -f qdrant

# Stop services
docker-compose down
```

## Code Style Guidelines

### C# Conventions

**Naming:**
- Classes/PascalCase: `ProjectService`, `DocumentRepository`
- Properties/PascalCase: `FileName`, `CreatedAt`
- Private fields/_camelCase: `_configuration`, `_logger`
- Parameters/camelCase: `projectId`, `cancellationToken`
- Constants/PascalCase: `MaxFileSize`, `DefaultTimeout`
- Enums/PascalCase: `DocumentStatus`, `FileExtensionType`
- Interfaces/I + PascalCase: `IRepository<T>`, `IApplicationDbContext`
- Async methods/Async suffix: `GetProjectAsync`, `SaveChangesAsync`

**Imports/Usings:**
- System imports first: `using System;`
- Microsoft imports second: `using Microsoft.EntityFrameworkCore;`
- Third-party imports third: `using Qdrant.Client;`
- Project imports last: `using Babel.Domain.Entities;`
- Sort alphabetically within each group
- Use file-scoped namespaces: `namespace Babel.Domain.Entities;`
- Enable implicit usings in all projects: `<ImplicitUsings>enable</ImplicitUsings>`

**Formatting:**
- Use C# 12+ features: pattern matching, switch expressions, primary constructors
- 4 spaces indentation (no tabs)
- Opening brace on new line for classes/methods
- Opening brace on same line for properties/if/for
- Max line length: 120 characters
- Use `var` when type is obvious, explicit otherwise
- Blank line between methods
- Single statement per line

**Types:**
- Always enable nullable reference types: `<Nullable>enable</Nullable>`
- Use `string?` for nullable strings, `string` for non-null
- Use `DateTime.UtcNow` for UTC, never `DateTime.Now`
- Use `Guid` for IDs, not `int` or `string`
- Use `List<T>` for mutable lists, `IEnumerable<T>` for read-only
- Prefer records over classes for immutable DTOs
- Use `int` for collection counts, `long` for file sizes/bytes

**Comments:**
- NO unnecessary comments - code should be self-documenting
- XML doc comments only for public APIs
- Use Spanish comments for business logic explanations
- One-line XML doc: `/// <summary>Nombre del proyecto.</summary>`
- Multi-line XML doc: start with `/// <summary>\n/// `

**Error Handling:**
- Use try-catch for external service calls (Qdrant, OCR, LLM)
- Log structured errors: `_logger.LogError(ex, "Error al procesar documento: {DocumentId}", documentId);`
- Create domain exceptions: `DocumentNotFoundException`, `ProjectAlreadyExistsException`
- Use `Result<T>` pattern in Application layer instead of exceptions
- Validate arguments early: `Guard.Against.Null(request, nameof(request));`
- Return meaningful error messages to clients

**Dependency Injection:**
- Constructor injection only (no property injection)
- Order constructor parameters: services first, then options, then logger
- Use `ILogger<T>` for all services
- Use scoped lifetime for DbContext, repositories, services
- Use singleton lifetime for clients, configuration, caching
- Use transient lifetime for lightweight services

### Entity Framework

**DbContext:**
- Use `DbSet<T>` with C# property syntax: `public DbSet<Project> Projects => Set<Project>();`
- Override `SaveChangesAsync` to update `UpdatedAt` timestamps
- Apply configurations from assembly: `modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());`
- Use `DbContextOptions<BabelDbContext>` constructor parameter

**Entities:**
- Inherit from `BaseEntity` (Id, CreatedAt, UpdatedAt)
- Use nullable reference types for navigation properties: `public Project? Project { get; set; }`
- Initialize collections in property: `public ICollection<Document> Documents { get; set; } = new List<Document>();`
- Use `Guid` for primary keys
- Region groupings for related properties (`#region Relación con Proyecto`)

**Queries:**
- Use `Include()` for eager loading: `context.Projects.Include(p => p.Documents).ToListAsync()`
- Use `AsNoTracking()` for read-only queries
- Use `FirstAsync()`/`SingleAsync()` for expected single results
- Use `FirstOrDefaultAsync()`/`SingleOrDefaultAsync()` for optional results

### Testing

**Test Structure:**
- Use xUnit + FluentAssertions
- Arrange-Act-Assert pattern with comments
- Fact: `[Fact]` for tests without parameters
- Theory: `[Theory]` with `[InlineData]` for parameterized tests
- Test class names: `ClassNameTests` (e.g., `ProjectTests`)
- Test method names: `Method_State_Expected` (e.g., `Constructor_ShouldInitializeWithDefaultValues`)

**Assertions:**
- Use FluentAssertions: `project.Id.Should().NotBeEmpty();`
- Be precise: `project.Documents.Should().HaveCount(1);`
- Use time tolerance: `project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));`
- Assert all relevant properties

### Blazor/MudBlazor

**Components:**
- File extension: `.razor`
- Page directive: `@page "/"`
- Inject services: `@inject ISnackbar Snackbar`
- Use `@code` block for C# logic
- Prefix component private fields with `_`: `_projects`, `_showDialog`
- Use `@bind-Value` for two-way binding
- Use `@bind-Visible` for MudDialog

**Event Handling:**
- Use lambda for simple handlers: `OnClick="@(() => _showDialog = false)"`
- Use separate method for complex logic: `OnClick="@CreateProject"`
- Event handlers use PascalCase: `OnCreate`, `OnCardClick`

**HTML/Razor:**
- Use MudBlazor components: `MudButton`, `MudText`, `MudGrid`
- Use MudGrid for responsive layouts: `xs="12" md="6"`
- Use CSS classes from MudBlazor: `mb-4`, `pa-4`, `d-flex`
- Comment blocks with `@* Comment *@`

### Configuration

**appsettings.json:**
- Section-based configuration: `ConnectionStrings`, `Qdrant`, `AzureComputerVision`
- Use `appsettings.local.json` for secrets (not committed)
- Use strong typing: `builder.Configuration.GetValue<int>("Qdrant:VectorSize", 1536)`
- Validate configuration on startup with custom validator
- Use environment-specific overrides: `appsettings.Development.json`

### Clean Architecture Layers

**Domain Layer (Babel.Domain):**
- No external dependencies
- Entities, ValueObjects, Enums, Domain Events
- Interfaces for repositories and services
- Business logic and invariants

**Application Layer (Babel.Application):**
- Commands, Queries, Handlers using MediatR
- DTOs for data transfer
- Interfaces for external services (AI, OCR, Storage)
- Result pattern for error handling

**Infrastructure Layer (Babel.Infrastructure):**
- EF Core DbContext and migrations
- Repository implementations
- External service implementations (Qdrant, OCR, AI)
- Dependency injection configuration

**Presentation Layer (Babel.API, Babel.WebUI):**
- API controllers or Blazor pages
- Minimal configuration, delegates to Application layer
- No business logic

### File Organization

```
src/Babel.Domain/
  Entities/
  Enums/
  ValueObjects/
  Interfaces/

src/Babel.Application/
  Commands/
  Queries/
  DTOs/
  Interfaces/

src/Babel.Infrastructure/
  Data/
  Repositories/
  Services/
  Jobs/

src/Babel.API/
  Controllers/
  Program.cs

src/Babel.WebUI/
  Pages/
  Components/
  Services/

tests/Babel.Domain.Tests/
  Entities/
  ValueObjects/
```

### Important Patterns

**Async/Await:**
- All I/O operations must be async
- Use `ConfigureAwait(false)` in library code
- Avoid `async void` except for event handlers

**Caching:**
- Use IMemoryCache for in-memory caching
- Cache keys: `project:{projectId}`, `document:{documentId}`
- Set reasonable expiration times

**Logging:**
- Use `ILogger<T>` for structured logging
- Log at appropriate levels: Debug, Information, Warning, Error
- Include context in log messages: `Processing document {DocumentId} in project {ProjectId}`
- Log performance metrics for slow operations

**External Services:**
- Always use async methods
- Implement retry policies with Polly
- Use circuit breakers for unreliable services
- Log all failures with context
- Provide graceful degradation when services are unavailable
