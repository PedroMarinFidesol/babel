# AGENTS.md

This file guides agentic coding assistants working in the Babel repository.

## Build/Test Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Run single test
dotnet test --filter "FullyQualifiedName~Constructor_ShouldInitializeWithDefaultValues"

# Run tests by class name
dotnet test --filter "FullyQualifiedName~ProjectTests"

# Database migration
dotnet ef migrations add MigrationName --project Babel.Infrastructure --startup-project Babel.API

# Apply migrations
dotnet ef database update --project Babel.Infrastructure --startup-project Babel.API

# Run API
cd src/Babel.API && dotnet run

# Run WebUI
cd src/Babel.WebUI && dotnet run
```

## Code Style Guidelines

### C# Conventions

**Naming:** Classes/Properties/Constants/Enums: PascalCase; private fields: _camelCase; parameters: camelCase; Interfaces: I + PascalCase; Async methods: Async suffix

**Imports/Usings:** System → Microsoft → third-party → project (alphabetical within groups); file-scoped namespaces; enable implicit usings

**Formatting:** C# 12+ features; 4 spaces; braces: classes/methods new line, properties/if same line; max 120 chars; var when obvious; blank lines between methods

**Types:** Nullable enabled; string? for nullable; DateTime.UtcNow only; Guid for IDs; List<T> mutable, IEnumerable<T> read-only; records for DTOs; int for counts, long for bytes

**Comments:** NO unnecessary comments; XML docs only for public APIs; Spanish for business logic

**Error Handling:** try-catch for external services; structured logging; domain exceptions; Result<T> pattern in Application; early validation

**Dependency Injection:** Constructor only; services → options → logger; ILogger<T> everywhere; scoped for DbContext/repos; singleton for clients/config; transient for lightweight

### Entity Framework

**DbContext:** DbSet<T> with C# property syntax; override SaveChangesAsync for UpdatedAt; ApplyConfigurationsFromAssembly; DbContextOptions<BabelDbContext> constructor

**Entities:** Inherit from BaseEntity; nullable navigation properties: `Project? Project`; initialize collections: `= new List<T>()`; Guid PKs; `#region Relación con Proyecto`

**Queries:** Include() for eager loading; AsNoTracking() for read-only; FirstAsync()/SingleAsync() for required; FirstOrDefaultAsync()/SingleOrDefaultAsync() for optional

### Testing

**Test Structure:** Use xUnit + FluentAssertions; Arrange-Act-Assert pattern with comments; `[Fact]` for no params, `[Theory]` with `[InlineData]`; class names: `ClassNameTests`; method names: `Method_State_Expected`

**Assertions:** Use FluentAssertions: `project.Id.Should().NotBeEmpty();`; Be precise: `project.Documents.Should().HaveCount(1);`; Use time tolerance: `project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));`; Assert all relevant properties

### Blazor/MudBlazor

**Components:** File extension: `.razor`; Page directive: `@page "/"`; Inject services: `@inject ISnackbar Snackbar`; Use `@code` block for C# logic; Prefix component private fields with `_`: `_projects`, `_showDialog`; Use `@bind-Value` for two-way binding; Use `@bind-Visible` for MudDialog

**Event Handling:** Use lambda for simple handlers: `OnClick="@(() => _showDialog = false)"`; Use separate method for complex logic: `OnClick="@CreateProject"`; Event handlers use PascalCase: `OnCreate`, `OnCardClick`

**HTML/Razor:** Use MudBlazor components: `MudButton`, `MudText`, `MudGrid`; Use MudGrid for responsive layouts: `xs="12" md="6"`; Use CSS classes from MudBlazor: `mb-4`, `pa-4`, `d-flex`; Comment blocks with `@* Comment *@`

### Configuration

**appsettings.json:** Section-based: `ConnectionStrings`, `Qdrant`, `AzureComputerVision`; `appsettings.local.json` for secrets (not committed); strong typing: `builder.Configuration.GetValue<int>("Qdrant:VectorSize", 1536)`; validate on startup; environment overrides

### Clean Architecture Layers

**Domain (Babel.Domain):** No external dependencies; Entities, ValueObjects, Enums, Domain Events; Interfaces for repositories/services; Business logic and invariants

**Application (Babel.Application):** Commands/Queries/Handlers using MediatR; DTOs for data transfer; Interfaces for external services (AI, OCR, Storage); Result pattern for error handling

**Infrastructure (Babel.Infrastructure):** EF Core DbContext and migrations; Repository implementations; External service implementations (Qdrant, OCR, AI); Dependency injection configuration

**Presentation (Babel.API, Babel.WebUI):** API controllers or Blazor pages; Minimal configuration, delegates to Application layer; No business logic

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

**Async/Await:** All I/O must be async; Use `ConfigureAwait(false)` in library code; Avoid `async void` except for event handlers

**Caching:** IMemoryCache for in-memory caching; Cache keys: `project:{projectId}`, `document:{documentId}`; Set reasonable expiration times

**Logging:** `ILogger<T>` for structured logging; Log at appropriate levels (Debug, Information, Warning, Error); Include context: `Processing document {DocumentId} in project {ProjectId}`; Log performance metrics for slow operations

**External Services:** Always use async methods; Implement retry policies with Polly; Use circuit breakers for unreliable services; Log all failures with context; Provide graceful degradation when services are unavailable
