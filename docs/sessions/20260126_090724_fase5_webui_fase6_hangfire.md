# Sesión 2026-01-26 09:07 - Fase 5 WebUI y Fase 6 Hangfire

## Resumen de la Sesión

Completé la integración de la WebUI para la Fase 5 (subida de documentos) y avancé significativamente en la Fase 6 (procesamiento asíncrono con Hangfire). Ahora los documentos se suben realmente a través de MediatR, se almacenan en el sistema de archivos, y se encolan automáticamente para procesamiento en segundo plano.

## Cambios Implementados

### Fase 5: Integración WebUI

#### 1. FileUpload.razor - Subida real de documentos
```razor
@using Babel.Application.Documents.Commands
@using Babel.Application.DTOs
@inject ISnackbar Snackbar
@inject IMediator Mediator

// Callback ahora devuelve DocumentDto en lugar de IBrowserFile
[Parameter]
public EventCallback<List<DocumentDto>> OnFilesUploaded { get; set; }

// Upload real usando MediatR
private async Task UploadFilesAsync()
{
    for (int i = 0; i < _selectedFiles.Count; i++)
    {
        var file = _selectedFiles[i];
        await using var stream = file.OpenReadStream(maxSize);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var command = new UploadDocumentCommand(ProjectId, file.Name, memoryStream);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
            uploadedDocuments.Add(result.Value!);
    }
}
```

#### 2. Detail.razor - Carga y eliminación de documentos
```csharp
// Cargar documentos desde BD al iniciar
protected override async Task OnInitializedAsync()
{
    await LoadProjectAndDocuments();
}

private async Task LoadProjectAndDocuments()
{
    var projectResult = await Mediator.Send(new GetProjectByIdQuery(ProjectId));
    if (projectResult.IsSuccess)
    {
        _project = projectResult.Value!;
        var documentsResult = await Mediator.Send(new GetDocumentsByProjectQuery(ProjectId));
        if (documentsResult.IsSuccess)
            _documents = documentsResult.Value!.ToList();
    }
}

// Eliminar documentos
private async Task DeleteDocument(DocumentDto document)
{
    var result = await Mediator.Send(new DeleteDocumentCommand(document.Id));
    if (result.IsSuccess)
    {
        _documents.Remove(document);
        _project.TotalDocuments = Math.Max(0, _project.TotalDocuments - 1);
    }
}
```

### Fase 6: Hangfire

#### 1. Paquetes instalados
- `Hangfire.Core 1.8.17` en Infrastructure
- `Hangfire.SqlServer 1.8.17` en Infrastructure
- `Hangfire.AspNetCore 1.8.17` en API

#### 2. HangfireOptions.cs
```csharp
public class HangfireOptions
{
    public const string SectionName = "Hangfire";
    public string DashboardPath { get; set; } = "/hangfire";
    public int WorkerCount { get; set; } = 2;
    public int PollingIntervalSeconds { get; set; } = 15;
    public int MaxRetryAttempts { get; set; } = 3;
}
```

#### 3. ITextExtractionService
```csharp
public interface ITextExtractionService
{
    Task<Result<string>> ExtractTextAsync(Guid documentId, CancellationToken cancellationToken = default);
}
```

#### 4. TextExtractionService
```csharp
public async Task<Result<string>> ExtractTextAsync(Guid documentId, CancellationToken cancellationToken = default)
{
    var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
    if (document is null)
        return Result.Failure<string>(DomainErrors.Document.NotFound);

    switch (document.FileType)
    {
        case FileExtensionType.TextBased:
            extractedText = await ExtractFromTextFileAsync(document.FilePath, cancellationToken);
            break;
        case FileExtensionType.Pdf:
            if (document.RequiresOcr)
                return Result.Failure<string>(DomainErrors.Document.RequiresOcr);
            extractedText = await ExtractFromPdfAsync(document.FilePath, cancellationToken);
            break;
        // ... más casos
    }

    document.Content = extractedText;
    document.Status = DocumentStatus.Completed;
    document.ProcessedAt = DateTime.UtcNow;
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return Result<string>.Success(extractedText);
}
```

#### 5. DocumentProcessingJob
```csharp
public class DocumentProcessingJob
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    [JobDisplayName("Extraer texto: {0}")]
    public async Task ProcessAsync(Guid documentId)
    {
        var result = await _textExtractionService.ExtractTextAsync(documentId);
        if (!result.IsSuccess && result.Error.Code == "Document.RequiresOcr")
        {
            // TODO: Encolar job de OCR
        }
    }
}
```

#### 6. IDocumentProcessingQueue
```csharp
public interface IDocumentProcessingQueue
{
    string EnqueueTextExtraction(Guid documentId);
    string EnqueueOcrProcessing(Guid documentId);
}
```

#### 7. Integración en UploadDocumentCommandHandler
```csharp
// Después de guardar en BD, encolar procesamiento
_documentRepository.Add(document);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// 9. Encolar procesamiento en segundo plano
_processingQueue?.EnqueueTextExtraction(document.Id);
```

#### 8. Program.cs - Configuración Hangfire
```csharp
// Hangfire Configuration
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
    {
        SchemaName = "HangFire"
    }));

// Add Hangfire Server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = hangfireOptions.WorkerCount;
    options.Queues = new[] { "default", "documents" };
});

// Map Hangfire Dashboard
app.MapHangfireDashboard(hangfireOptions.DashboardPath, new DashboardOptions
{
    DashboardTitle = "Babel - Jobs Dashboard"
});
```

### Errores de dominio añadidos
```csharp
public static readonly Error RequiresOcr = new(
    "Document.RequiresOcr",
    "El documento requiere procesamiento OCR.");

public static readonly Error ExtractionFailed = new(
    "Document.ExtractionFailed",
    "Error al extraer texto del documento.");
```

## Problemas Encontrados y Soluciones

### 1. ProjectDto no es record
**Problema:** Error CS8858 al usar `with` syntax en ProjectDto
```csharp
_project = _project with { TotalDocuments = _project.TotalDocuments + 1 };
```
**Solución:** ProjectDto es una clase, no un record. Usar asignación directa:
```csharp
_project.TotalDocuments += 1;
```

### 2. Result.Failure<T> vs Result<T>.Failure
**Problema:** No se puede convertir Result a Result<string>
**Solución:** Usar el método estático de Result base:
```csharp
// Incorrecto
return Result<string>.Failure(DomainErrors.Document.NotFound);

// Correcto
return Result.Failure<string>(DomainErrors.Document.NotFound);
```

### 3. FileExtensionType enum values
**Problema:** El enum usa valores agrupados (TextBased, ImageBased), no individuales (Text, Markdown, Json)
**Solución:** Usar los valores correctos del enum existente:
```csharp
case FileExtensionType.TextBased:  // No Text, Markdown, Json
case FileExtensionType.ImageBased: // No Image
case FileExtensionType.Pdf:
case FileExtensionType.OfficeDocument: // No Word, Excel
```

### 4. AddHangfire no encontrado en Infrastructure
**Problema:** El método `AddHangfire` está en Hangfire.AspNetCore, no en Hangfire.Core
**Solución:** Mover la configuración de Hangfire de DependencyInjection.cs a Program.cs del API project que sí tiene la referencia a Hangfire.AspNetCore

### 5. IDashboardAuthorizationFilter no encontrado
**Problema:** Faltaba using para el namespace de Dashboard
**Solución:** Añadir `using Hangfire.Dashboard;`

## Archivos Creados

```
src/Babel.Application/Interfaces/
├── IBackgroundJobService.cs
├── IDocumentProcessingQueue.cs
└── ITextExtractionService.cs

src/Babel.Infrastructure/Configuration/
└── HangfireOptions.cs

src/Babel.Infrastructure/Jobs/
└── DocumentProcessingJob.cs

src/Babel.Infrastructure/Services/
├── DocumentProcessingQueue.cs
├── HangfireBackgroundJobService.cs
└── TextExtractionService.cs
```

## Archivos Modificados

- `src/Babel.API/Program.cs` - Configuración Hangfire
- `src/Babel.API/Babel.API.csproj` - Paquete Hangfire.AspNetCore
- `src/Babel.Infrastructure/Babel.Infrastructure.csproj` - Paquetes Hangfire.Core y Hangfire.SqlServer
- `src/Babel.Infrastructure/DependencyInjection.cs` - Registro de servicios
- `src/Babel.Application/Common/DomainErrors.cs` - Nuevos errores
- `src/Babel.Application/Documents/Commands/UploadDocumentCommandHandler.cs` - Encolar job
- `src/Babel.WebUI/Components/Documents/FileUpload.razor` - Upload real
- `src/Babel.WebUI/Pages/Projects/Detail.razor` - Carga y eliminación de documentos
- `docs/PLAN_DESARROLLO.md` - Actualizado estado de fases

## Próximos Pasos

1. **Fase 6 - Pendiente:**
   - Implementar extracción de PDF con PdfPig o similar
   - Implementar extracción de Office con DocumentFormat.OpenXml
   - Crear OcrProcessingJob para imágenes
   - UI de revisión de OCR

2. **Fase 7 - Vectorización:**
   - Chunking de documentos
   - Generación de embeddings
   - Almacenamiento en Qdrant

## Comandos Útiles

```bash
# Instalar paquetes Hangfire
dotnet add src/Babel.Infrastructure/Babel.Infrastructure.csproj package Hangfire.Core --version 1.8.17
dotnet add src/Babel.Infrastructure/Babel.Infrastructure.csproj package Hangfire.SqlServer --version 1.8.17
dotnet add src/Babel.API/Babel.API.csproj package Hangfire.AspNetCore --version 1.8.17

# Compilar y probar
dotnet build
dotnet test --verbosity minimal
```

## Lecciones Aprendidas

1. **Blazor Server vs WebAssembly:** En Blazor Server se puede usar MediatR directamente porque todo corre en el servidor, no se necesita HttpClient
2. **Separación de Hangfire:** El paquete AspNetCore va en el proyecto API, Core y SqlServer pueden ir en Infrastructure
3. **Result pattern:** Usar `Result.Failure<T>()` desde la clase base, no `Result<T>.Failure()`
4. **Enums existentes:** Siempre verificar los valores del enum antes de usarlos en switch

## Estado Final del Proyecto

- [x] 160 tests pasando (27 domain + 77 application + 56 infrastructure)
- [x] Fase 5 completada (WebUI integrada)
- [x] Fase 6 parcialmente completada (Hangfire configurado, jobs básicos)
- [x] Dashboard Hangfire disponible en `/hangfire`
- [x] Documentos se procesan automáticamente después de upload
- [ ] Pendiente: Extracción de PDF y Office
- [ ] Pendiente: OCR para imágenes
