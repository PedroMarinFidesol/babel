# CLAUDE.md

Este archivo proporciona orientación a Claude Code (claude.ai/code) cuando trabaja con código en este repositorio.

## Gestión de Sesiones y Documentación

### Skill: /end-session

**Para finalizar una sesión de trabajo**, usa el comando:

```
/end-session
```

Este skill automáticamente:
1. ✅ Crea el documento de sesión con timestamp en `docs/sessions/`
2. ✅ Actualiza el diario de desarrollo HTML (`docs/dev-diary.html`)
3. ✅ Actualiza CLAUDE.md con referencia a la nueva sesión

**Nota:** El commit y push a Git debe hacerse manualmente por el usuario cuando lo desee.

### Documentación de Sesiones de Trabajo

El skill `/end-session` creará un documento de sesión en `docs/sessions/` con el siguiente formato:

**Nombre del archivo:** `YYYYMMDD_HHMMSS_descripcion_breve.md`

Ejemplo: `20260124_112606_proyecto_base_y_correccion_qdrant.md`

**Contenido requerido:**
1. **Título con fecha y hora:** Sesión YYYY-MM-DD HH:MM - Descripción
2. **Resumen de la sesión:** Breve descripción de qué se hizo
3. **Cambios implementados:** Lista detallada de archivos creados/modificados con snippets de código relevantes
4. **Problemas encontrados y soluciones:** Errores encontrados, causas raíz y cómo se resolvieron
5. **Desafíos técnicos:** Problemas de compatibilidad, decisiones de diseño, alternativas consideradas
6. **Configuración final:** Estado de appsettings.json, paquetes instalados, etc.
7. **Próximos pasos:** Qué falta por hacer y en qué orden
8. **Comandos útiles:** Comandos relevantes ejecutados durante la sesión
9. **Lecciones aprendidas:** Aprendizajes clave de la sesión
10. **Estado final del proyecto:** Checklist de lo completado

**Propósito:**
- Documentar decisiones técnicas tomadas
- Facilitar la continuación del trabajo en futuras sesiones
- Crear un historial de evolución del proyecto
- Servir como referencia para troubleshooting

## Reglas de Seguridad - IMPORTANTE

**NUNCA incluir credenciales, passwords, API keys o secrets en archivos que se commitean a Git.**

### Archivos permitidos para secrets:
- `appsettings.local.json` (está en .gitignore)
- Variables de entorno
- User secrets de .NET (`dotnet user-secrets`)

### Archivos PROHIBIDOS para secrets:
- `appsettings.json` - NUNCA poner passwords, API keys o connection strings con credenciales
- `appsettings.Development.json`
- Cualquier archivo `.cs`, `.razor`, etc.

### Qué debe ir en cada archivo:

**appsettings.json** (se commitea):
```json
"ConnectionStrings": {
  "DefaultConnection": ""  // Vacío, sin credenciales
},
"OpenAI": {
  "ApiKey": ""  // Vacío
}
```

**appsettings.local.json** (NO se commitea):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BabelDb;User Id=sa;Password=REAL_PASSWORD;"
},
"OpenAI": {
  "ApiKey": "sk-proj-REAL_API_KEY"
}
```

## Descripción General del Proyecto

**Babel** es un sistema de gestión documental con capacidades de IA y OCR. Está construido como un monolito modular usando .NET Core y principios de Clean Architecture.

**Características Principales:**
- **Organización basada en proyectos**: Los documentos se agrupan en proyectos (con nombre e ID)
- **Carga por lotes de documentos**: Se pueden subir múltiples archivos a la vez
- **Procesamiento inteligente**: Los archivos se procesan directamente en BD o se envían a OCR según su extensión
- **Revisión de OCR**: Los archivos procesados por OCR pueden revisarse y editarse
- **Búsqueda semántica**: Embeddings vectoriales para recuperación inteligente de documentos
- **Chat potenciado por IA**: Chat con patrón RAG que devuelve respuestas con referencias a documentos fuente
- **Procesamiento asíncrono de trabajos**: Jobs en segundo plano para OCR y vectorización

## Stack Tecnológico

### Backend y Arquitectura
- **.NET 10.0** con Clean Architecture (Domain, Application, Infrastructure, Presentation)
- **Blazor Server** para UI
- **MediatR** para patrón CQRS (Commands/Queries)
- **Entity Framework Core 10.0.2** para acceso a datos

### Almacenamiento de Datos
- **SQL Server** (Docker): Base de datos relacional principal para proyectos, metadatos de documentos y datos estructurados
- **Qdrant** (Docker): Base de datos vectorial que almacena embeddings de documentos con referencias a archivos
- **Sistema de Archivos/NAS**: Almacenamiento físico de documentos

### IA y OCR
- **Semantic Kernel**: Framework de orquestación de LLMs que soporta múltiples proveedores:
  - **Ollama** (modelos locales)
  - **OpenAI API** (GPT-4, text-embedding-ada-002)
  - **Google Gemini API**
- **Azure Computer Vision** (Docker): Servicio OCR para documentos basados en imágenes

### Infraestructura
- **Hangfire**: Procesamiento de trabajos en segundo plano (OCR, vectorización)
- **Docker**: Contenedorización de SQL Server y Qdrant

## Arquitectura

### Capas de Clean Architecture

```
Domain (Core) → Application (Casos de Uso) → Infrastructure (Implementaciones) → Presentation (API/UI)
```

**Regla de Dependencias**: Las dependencias apuntan hacia adentro. Domain no tiene dependencias. Infrastructure depende de interfaces de Application.

### Módulos Clave

1. **Módulo de Proyectos**: Gestión de proyectos (CRUD, listado de proyectos con conteo de archivos)
2. **Módulo de Documentos**: CRUD de documentos, carga por lotes, gestión de metadatos
3. **Módulo OCR**: Procesamiento de imágenes, extracción de texto y revisión/edición de OCR
4. **Módulo de Vectorización**: Generación de embeddings y almacenamiento en Qdrant con referencias a archivos
5. **Módulo de Búsqueda**: Búsqueda de texto tradicional + búsqueda vectorial semántica
6. **Módulo de IA**: Chat basado en RAG que devuelve respuestas con referencias a documentos fuente
7. **Módulo de Jobs**: Procesadores en segundo plano de Hangfire para OCR y vectorización

## Principios de Diseño

Este proyecto sigue tres pilares fundamentales de diseño de software:

### Domain-Driven Design (DDD)

- **Ubiquitous Language**: Usar terminología consistente entre código y dominio de negocio (Project, Document, etc.)
- **Bounded Contexts**: Cada módulo representa un contexto delimitado con responsabilidades claras
- **Entities**: Objetos con identidad única (Project, Document)
- **Value Objects**: Objetos inmutables sin identidad (ej. FileExtension, DocumentStatus)
- **Aggregates**: Project es el aggregate root que gestiona sus Documents
- **Domain Events**: Para comunicación desacoplada entre módulos
- **Repositories**: Abstracciones para persistencia de aggregates

### Clean Architecture

- **Independencia de frameworks**: El dominio no depende de EF, ASP.NET u otros frameworks
- **Testabilidad**: Las capas internas son fácilmente testeables sin dependencias externas
- **Independencia de UI**: La lógica de negocio no conoce si se usa Blazor, API REST u otra UI
- **Independencia de BD**: Se puede cambiar SQL Server por otra BD sin afectar el dominio
- **Regla de dependencia**: Las dependencias solo apuntan hacia adentro (Domain ← Application ← Infrastructure ← Presentation)

### Clean Code

- **Nombres significativos**: Variables, métodos y clases con nombres que revelen intención
- **Funciones pequeñas**: Métodos que hacen una sola cosa y la hacen bien
- **Sin comentarios innecesarios**: El código debe ser autoexplicativo
- **DRY (Don't Repeat Yourself)**: Evitar duplicación de lógica
- **SOLID**:
  - **S**ingle Responsibility: Cada clase tiene una única razón para cambiar
  - **O**pen/Closed: Abierto para extensión, cerrado para modificación
  - **L**iskov Substitution: Las clases derivadas deben ser sustituibles por sus bases
  - **I**nterface Segregation: Interfaces específicas en lugar de una general
  - **D**ependency Inversion: Depender de abstracciones, no de implementaciones
- **Boy Scout Rule**: Dejar el código mejor de como lo encontraste

## Comandos de Desarrollo

### Compilar y Ejecutar

```bash
# Compilar toda la solución
dotnet build

# Ejecutar la aplicación
cd Babel.API
dotnet run

# Ejecutar tests
dotnet test
```

### Migraciones de Base de Datos

```bash
# Crear nueva migración
dotnet ef migrations add MigrationName --project Babel.Infrastructure --startup-project Babel.API

# Aplicar migraciones
dotnet ef database update --project Babel.Infrastructure --startup-project Babel.API

# Revertir a una migración específica
dotnet ef database update PreviousMigrationName --project Babel.Infrastructure --startup-project Babel.API
```

### Servicios Docker

```bash
# Iniciar SQL Server y Qdrant
docker-compose up -d

# Detener servicios
docker-compose down

# Ver logs
docker-compose logs -f sqlserver
docker-compose logs -f qdrant

# Verificar SQL Server
docker exec -it babel-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"

# Verificar colecciones de Qdrant
curl http://localhost:6333/collections
```

### Ollama (Opcional)

```bash
# Descargar modelos para LLM local
ollama pull llama2
ollama pull nomic-embed-text

# Listar modelos instalados
ollama list

# Probar modelo
ollama run llama2 "Hello"
```

## Estructura del Proyecto

```
src/
├── Babel.Domain/           - Entidades, ValueObjects, Interfaces de Dominio (sin dependencias)
│   ├── Entities/          - Project, Document, DocumentChunk, BaseEntity
│   └── Enums/             - DocumentStatus, FileExtensionType
├── Babel.Application/      - Commands, Queries, DTOs, Interfaces de Servicios (depende de Domain)
│   ├── Commands/          - CreateProjectCommand, UploadDocumentsCommand, ProcessOcrCommand
│   ├── Queries/           - GetProjectsQuery, SearchDocumentsQuery, ChatQuery
│   └── DTOs/              - ProjectDto, DocumentDto, ChatResponseDto (con referencias a documentos)
├── Babel.Infrastructure/   - EF DbContext, Repositorios, Implementaciones de Servicios Externos
│   ├── Data/              - BabelDbContext, Migrations
│   ├── Repositories/      - ProjectRepository, DocumentRepository, QdrantRepository
│   ├── Services/          - OCR (con revisión), AI (Semantic Kernel), Storage, Vectorization
│   └── Jobs/              - OcrProcessingJob, DocumentVectorizationJob
├── Babel.API/              - Controladores REST API, Program.cs
└── Babel.WebUI/            - Páginas y Componentes de Blazor Server (futuro)
    ├── Pages/
    │   ├── Index.razor           - Vista de tarjetas de proyectos (nombre + cantidad de archivos)
    │   └── Project/
    │       └── Detail.razor      - Detalle de proyecto con chat, lista de archivos, formulario de carga
    └── Components/
        ├── ChatWindow.razor      - Interfaz de chat con referencias a documentos
        ├── FileList.razor        - Listado de documentos
        └── FileUpload.razor      - Componente de carga por lotes

tests/
└── Babel.Domain.Tests/     - Tests unitarios para entidades de dominio (xUnit + FluentAssertions)
    └── Entities/          - ProjectTests, DocumentTests, DocumentChunkTests
```

## Flujos de Trabajo Clave

### Flujo de Carga y Procesamiento de Documentos

1. Usuario sube documentos (por lotes) a un proyecto → Almacenados en sistema de archivos
2. Extensión de archivo analizada: archivos de texto → almacenamiento directo en BD, imágenes → cola de OCR
3. Metadatos guardados en SQL Server (vinculados al proyecto)
4. **Para archivos de texto**: Contenido extraído inmediatamente, job de vectorización activado
5. **Para imágenes**: Job de OCR de Hangfire activado
6. OCR extrae texto → Disponible para revisión/edición → Actualiza SQL Server
7. Semantic Kernel genera embeddings para todos los documentos procesados
8. Embeddings almacenados en Qdrant con referencias a archivos de documentos (no contenido completo)

### Flujo de Búsqueda Semántica

1. Usuario ingresa consulta de búsqueda
2. Consulta convertida a embedding vía Semantic Kernel
3. Búsqueda de similitud vectorial en Qdrant
4. IDs de documentos devueltos
5. Metadatos obtenidos de SQL Server
6. Resultados mostrados con puntuaciones de relevancia

### Flujo de Chat RAG (Limitado por Proyecto)

1. Usuario hace pregunta dentro del contexto de un proyecto
2. Pregunta vectorizada vía Semantic Kernel
3. Búsqueda de similitud vectorial en Qdrant (filtrada por proyecto)
4. Recuperar referencias de archivos de resultados de Qdrant
5. Obtener contenido de documentos de SQL Server usando referencias de archivos
6. Fragmentos de documentos relevantes ensamblados como contexto
7. Contexto + pregunta enviados al LLM vía Semantic Kernel
8. Respuesta del LLM + lista de IDs de documentos referenciados devueltos
9. Respuesta y referencias a documentos mostradas en UI de Blazor

## Configuración

### Secciones Clave de appsettings.json

- **ConnectionStrings**: SQL Server (principal + Hangfire)
- **Qdrant**: Endpoint y nombre de colección
- **AzureComputerVision**: Endpoint local y API key
- **SemanticKernel**: Selección de proveedor (Ollama/OpenAI/Gemini) con credenciales
- **FileStorage**: Ruta base y límites de tamaño de archivo
- **Hangfire**: Ruta del dashboard y cantidad de workers

## Notas Importantes de Desarrollo

### Modelo de Dominio

El modelo de dominio sigue un patrón de chunking para vectorización, donde cada documento se divide en fragmentos que se almacenan individualmente en Qdrant.

```
┌──────────────┐       ┌──────────────────┐       ┌─────────────────┐
│   PROJECT    │ 1   N │     DOCUMENT     │ 1   N │ DOCUMENT_CHUNK  │
├──────────────┤───────├──────────────────┤───────├─────────────────┤
│ Id           │       │ Id               │       │ Id              │
│ Name         │       │ ProjectId (FK)   │       │ DocumentId (FK) │
│ Description  │       │ FileName         │       │ ChunkIndex      │
│ CreatedAt    │       │ FilePath (NAS)   │       │ Content         │
│ UpdatedAt    │       │ FileSizeBytes    │       │ QdrantPointId ──┼──→ Qdrant
└──────────────┘       │ Content          │       │ TokenCount      │
                       │ IsVectorized     │       │ PageNumber      │
                       │ Status           │       └─────────────────┘
                       └──────────────────┘
```

**Entidad Project:**
- Id (Guid)
- Name (string)
- Description (string?) - descripción opcional del proyecto
- CreatedAt, UpdatedAt
- Navigation: List<Document>

**Entidad Document:**
- Id (Guid)
- ProjectId (FK)
- **Información del archivo físico:**
  - FileName (string) - nombre original del archivo
  - FileExtension (string) - extensión incluyendo el punto (.pdf)
  - FilePath (string) - ruta relativa en NAS
  - FileSizeBytes (long) - tamaño en bytes
  - ContentHash (string) - SHA256 para detectar duplicados
  - MimeType (string) - tipo MIME (application/pdf, image/png)
- **Clasificación:**
  - FileType (enum: Unknown, TextBased, ImageBased, Pdf, OfficeDocument)
- **Estado de procesamiento:**
  - Status (enum: Pending, Processing, Completed, Failed, PendingReview)
  - RequiresOcr (bool)
  - OcrReviewed (bool)
  - ProcessedAt (DateTime?)
- **Contenido extraído:**
  - Content (string?) - texto extraído del documento
- **Vectorización:**
  - IsVectorized (bool) - indica si está en Qdrant
  - VectorizedAt (DateTime?)
- Navigation: Project, List<DocumentChunk>

**Entidad DocumentChunk (nueva):**
- Id (Guid)
- DocumentId (FK)
- **Posición del chunk:**
  - ChunkIndex (int) - índice dentro del documento (0, 1, 2...)
  - StartCharIndex (int) - posición inicio en Content original
  - EndCharIndex (int) - posición fin en Content original
- **Contenido:**
  - Content (string) - texto del chunk
  - TokenCount (int) - tokens estimados
- **Referencia a Qdrant:**
  - QdrantPointId (Guid) - ID del punto en Qdrant
- **Metadatos opcionales:**
  - PageNumber (string?) - número de página si aplica
  - SectionTitle (string?) - título de sección si se detecta
- Navigation: Document

### Decisiones de Diseño del Modelo

1. **Sin versionado de documentos**: Los archivos se sobrescriben al actualizar
2. **Chunking para RAG**: Los documentos se dividen en fragmentos para mejor precisión en búsqueda semántica
3. **Hard delete**: Borrado físico inmediato (sin papelera)
4. **Asociación con Qdrant**: Cada chunk tiene su propio QdrantPointId para búsqueda vectorial

### Requisitos de UI

**Página Principal (Index):**
- Mostrar proyectos como tarjetas
- Cada tarjeta muestra: Nombre del proyecto + cantidad de documentos
- Click en tarjeta → navegar a detalle de proyecto

**Página de Detalle de Proyecto:**
- Tres secciones principales:
  1. **Ventana de Chat**: Interfaz de chat RAG completa con respuestas en streaming
  2. **Lista de Archivos**: Tabla ordenable/filtrable de documentos del proyecto con estado
  3. **Formulario de Carga**: Arrastrar y soltar o explorar para carga por lotes
- Las respuestas del chat deben incluir referencias clicables a documentos fuente

### Integración de Semantic Kernel

- Usar el patrón **Kernel Builder** para configurar proveedores
- Soportar **cambio de plugins** en tiempo de ejecución (usuario puede elegir Ollama vs OpenAI)
- Implementar **políticas de reintentos** para llamadas a LLM
- Transmitir respuestas en streaming para endpoints de chat
- **Implementación RAG**: Devolver tanto el texto de respuesta como la lista de IDs de documentos fuente

### Operaciones Vectoriales con Qdrant

- La colección debe crearse con las dimensiones vectoriales correctas (coincidir con modelo de embedding)
- Almacenar **referencias a archivos** en el payload de Qdrant, no el contenido completo del documento
- Usar **filtrado de payload** para búsquedas limitadas por proyecto (filtrar por projectId)
- Implementar **operaciones por lotes** para vectorización masiva
- La búsqueda vectorial devuelve IDs/referencias de documentos → obtener contenido real de SQL Server
- Monitorear tamaño de colección e implementar estrategia de archivo

### Estrategia de Procesamiento de Archivos

**Enrutamiento basado en extensión:**
- **Archivos de texto** (.txt, .md, .json, .xml): Extracción de contenido directa, sin OCR
- **Archivos de imagen** (.jpg, .png, .tiff, .bmp): Enrutar a cola de OCR
- **PDF**: Verificar si es basado en texto o imagen, enrutar según corresponda
- **Documentos Office** (.docx, .xlsx): Extraer texto directamente vía biblioteca

**Flujo de Revisión de OCR:**
- Después de completar OCR, marcar documento como "Pending Review"
- Proporcionar UI para mostrar resultado de OCR con edición inline
- Guardar contenido revisado y marcar como "Reviewed"
- Solo vectorizar después de que la revisión esté completa (o auto-vectorizar si revisión deshabilitada)

### Jobs de Hangfire

- Todos los jobs deben ser **idempotentes** (seguros para reintentar)
- Usar **parámetros de job** para pasar IDs de documentos, no objetos completos
- Implementar **seguimiento de progreso** para jobs de OCR de larga duración
- Establecer **políticas de reintentos** apropiadas (3 reintentos con backoff exponencial)
- **OcrProcessingJob**: Procesar documento individual, marcar para revisión
- **DocumentVectorizationJob**: Generar embeddings y almacenar en Qdrant con referencia a archivo

### Manejo de Errores

- Crear excepciones de dominio personalizadas (ej. `DocumentNotFoundException`)
- Usar **patrón Result** para capa de Application (evitar lanzar excepciones en casos de uso)
- Registrar todos los fallos de servicios externos (OCR, LLM, Qdrant) con contexto
- Implementar patrón **circuit breaker** para APIs externas

## Convenciones de Código

- **Async/Await**: Todas las operaciones I/O deben ser asíncronas
- **Inyección de Dependencias**: Solo inyección por constructor
- **Nombrado**: PascalCase para clases/métodos, camelCase para parámetros/variables
- **Logging**: Usar `ILogger<T>` en todos los servicios, logging estructurado para eventos clave
- **Entity Framework**: Usar migraciones para todos los cambios de esquema, nunca modificar base de datos directamente

## Estrategia de Testing

- **Tests Unitarios**: Lógica de dominio y handlers de Application (mockear infraestructura)
- **Tests de Integración**: Implementaciones de repositorios, servicios externos
- **Componentes Blazor**: Usar bUnit para testing de componentes
- **End-to-End**: Probar flujo completo de carga de documento → OCR → búsqueda

## Puntos de Acceso

Cuando la aplicación está ejecutándose:

- **Blazor UI**: https://localhost:5001
- **Hangfire Dashboard**: https://localhost:5001/hangfire
- **Qdrant Dashboard**: http://localhost:6333/dashboard
- **SQL Server**: localhost,1433 (sa/YourStrong@Passw0rd)

## Problemas Comunes

### Conexión a SQL Server
Si la conexión falla, asegurar que el contenedor Docker esté ejecutándose y TrustServerCertificate=True esté configurado.

### Colección de Qdrant No Encontrada
Crear colección manualmente o implementar auto-creación en startup con dimensiones correctas.

### Conexión a Ollama Rechazada
Verificar que Ollama esté ejecutándose: `ollama serve` y que los modelos estén descargados.

### Jobs de Hangfire No se Procesan
Verificar conexión a SQL Server para base de datos de Hangfire y verificar que worker count > 0.

### Error: UriFormatException en QdrantClient
**Síntoma:** `System.UriFormatException: 'Invalid URI: The hostname could not be parsed.'` en DependencyInjection.cs

**Causa:** QdrantClient versión 1.16.1 requiere un objeto `Uri`, no acepta strings directamente.

**Solución:** ✅ Resuelto
```csharp
// Correcto
var qdrantEndpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
Uri qdrantUri = new Uri(qdrantEndpoint);
services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantUri));

// Incorrecto
services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantEndpoint));
```

## Historial de Sesiones

Para revisar el historial completo de desarrollo y decisiones técnicas, consulta los documentos de sesión en `docs/sessions/`:

- **20260201_214947_vectorizacion_tests_docs_chat_ui.md** - Fix concurrencia vectorización, documentación VECTORIZATION.md, 40 tests nuevos (total: 234 tests)
- **20260130_170204_servicio_chat_rag.md** - Fase 9: Servicio Chat RAG con SearchAsync en Qdrant, IChatService, SemanticKernelChatService, ChatQuery MediatR (111 tests)
- **20260126_162831_bugfix_qdrant_chunking.md** - Bugfix: conexión Qdrant gRPC (puerto 6334 vs 6333) y bucle infinito en ChunkingService
- **20260126_090724_fase5_webui_fase6_hangfire.md** - Fase 5 WebUI integrada con MediatR, Fase 6 Hangfire configurado con dashboard y jobs de procesamiento (total: 160 tests)
- **20260125_202951_fase3_crud_proyectos.md** - Fase 3: CRUD completo de proyectos, ProjectsController con 6 endpoints REST, SearchProjectsQuery, 30 tests de handlers (total: 88)
- **20260125_193629_fase2_cqrs_mediatr.md** - Fase 2: Patron Result, ICommand/IQuery, Behaviors (Logging, Validation, ExceptionHandling), Repositorios con Unit of Work, 31 tests
- **20260125_164840_fase1_infraestructura.md** - Fase 1: Docker Compose, migraciones EF, QdrantInitializationService, ConfigurationValidator, PLAN_DESARROLLO.md
- **20260125_151330_healthcheck_lazy_load.md** - Health checks bajo demanda y correccion de carga de appsettings.local.json en WebUI
- **20260125_105026_layout_webui_mudblazor.md** - Layout inicial de Babel.WebUI con MudBlazor 8.0, componentes de chat, upload y tarjetas de proyecto
- **20260125_101719_diseno_entidades_dominio.md** - Diseño de entidades de dominio con DocumentChunk para chunking RAG
- **20260125_101406_frontend_skill_mudblazor.md** - Creación de skill especializado para desarrollo frontend con MudBlazor
- **20260125_093956_configuracion_secrets_appsettings.md** - Configuración de secrets con appsettings.local.json
- **20260124_112606_proyecto_base_y_correccion_qdrant.md** - Creación de la estructura base del proyecto con Clean Architecture y corrección del error de QdrantClient

Cada documento de sesión contiene:
- Cambios implementados con código
- Problemas encontrados y soluciones
- Comandos ejecutados
- Lecciones aprendidas
- Estado del proyecto al finalizar la sesión

## Estado del Roadmap

**Fase 1 Completada:** ✅ Infraestructura y Base de Datos
- Clean Architecture con 4 capas implementada
- Docker Compose (SQL Server, Qdrant, Azure OCR)
- Health checks para SQL Server, Qdrant y Azure OCR
- Migración EF con DocumentChunk y campos completos
- Servicio de inicialización de Qdrant
- Clases de configuración (Options) y validador

**Fase 2 Completada:** ✅ CQRS con MediatR
- Patron Result y Error para manejo funcional de errores
- Interfaces ICommand/IQuery con handlers
- Behaviors: LoggingBehavior, ValidationBehavior, ExceptionHandlingBehavior
- Repositorios con Unit of Work (IRepository, IUnitOfWork)
- 31 tests unitarios de behaviors y common

**Fase 3 Completada:** ✅ Gestión de Proyectos (CRUD)
- Commands: CreateProject, UpdateProject, DeleteProject
- Queries: GetProjects, GetProjectById, SearchProjects
- ProjectsController con endpoints REST completos
- WebUI conectada a datos reales via MediatR (sin MockDataService)
- 30 tests unitarios de handlers

**Fase 4 Completada:** ✅ Almacenamiento de Archivos (NAS)
- IStorageService interface con LocalFileStorageService
- Seguridad: sanitización de nombres, prevención path traversal
- Hash SHA256 para deduplicación de archivos
- 15 tests de integración con archivos temporales

**Fase 5 Completada:** ✅ Subida de Documentos
- Commands: UploadDocument, DeleteDocument
- Queries: GetDocumentsByProject, GetDocumentById, GetDocumentContent
- DocumentsController con endpoints REST
- IFileTypeDetector para clasificación de archivos
- FileUpload.razor conectado con MediatR (subida real)
- Detail.razor con carga y eliminación de documentos
- 33 tests unitarios de handlers de documentos

**Fase 6 En Progreso:** ⏳ Hangfire
- Paquetes Hangfire.Core, Hangfire.SqlServer, Hangfire.AspNetCore
- Dashboard en /hangfire
- DocumentProcessingJob con reintentos automáticos
- TextExtractionService para archivos de texto
- Jobs encolados automáticamente después de upload
- Pendiente: Extracción de PDF y Office, OCR

**Fase 9 En Progreso:** ⏳ Chat RAG
- IVectorStoreService.SearchAsync() con filtro por proyecto
- VectorSearchResult record para resultados de búsqueda
- IChatService con ChatAsync y ChatStreamAsync
- SemanticKernelChatService implementa flujo RAG completo
- Chat Completion configurado (OpenAI/Ollama)
- ChatQuery/ChatQueryHandler/ChatQueryValidator de MediatR
- DomainErrors.Chat con errores específicos
- Pendiente: Conectar ChatWindow.razor, endpoint REST, tests

**Total Tests:** 111 (27 domain + 84 application)
