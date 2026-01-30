# Plan de Desarrollo - Babel

## Visión General

Este documento define el roadmap completo para desarrollar Babel, un sistema de gestión documental con capacidades de IA y OCR. El desarrollo sigue un enfoque incremental con casos de uso bien definidos.

---

## Estado Actual (Enero 2026)

### Completado
- [x] Estructura Clean Architecture (Domain, Application, Infrastructure, API, WebUI)
- [x] Entidades de dominio: Project, Document, DocumentChunk
- [x] DbContext con EF Core 10.0 y migración inicial
- [x] Health checks (SQL Server, Qdrant, Azure OCR)
- [x] Layout WebUI con Blazor Server + MudBlazor 8.0
- [x] Componentes UI básicos (ProjectCard, ChatWindow, FileUpload)
- [x] **FASE 1:** Docker Compose (SQL Server, Qdrant, Azure OCR)
- [x] **FASE 1:** Migración EF con DocumentChunk y campos completos
- [x] **FASE 1:** Servicio de inicialización de Qdrant
- [x] **FASE 1:** Clases de configuración (Options) y validador
- [x] **FASE 1:** appsettings.json con todas las secciones
- [x] **FASE 2:** MediatR con CQRS (Commands, Queries, Handlers)
- [x] **FASE 2:** Pipeline behaviors (Validation, Logging, ExceptionHandling)
- [x] **FASE 2:** Patrón Result<T> y Error para manejo funcional
- [x] **FASE 2:** Repositorios base con Unit of Work
- [x] **FASE 3:** CRUD completo de Proyectos (Commands + Queries + API + UI)
- [x] **FASE 3:** ProjectsController con endpoints REST
- [x] **FASE 3:** WebUI conectada a datos reales via MediatR
- [x] **FASE 4:** IStorageService y LocalFileStorageService
- [x] **FASE 4:** Configuración FileStorage con validaciones
- [x] **FASE 5:** Subida de documentos (UploadDocumentCommand + API)
- [x] **FASE 5:** DocumentsController con endpoints REST
- [x] **FASE 5:** FileUpload.razor conectado a MediatR
- [x] **FASE 5:** Listado y eliminación de documentos en WebUI
- [x] **FASE 6:** Hangfire configurado en API y WebUI
- [x] **FASE 6:** AddHangfireServices centralizado en DependencyInjection
- [x] **FASE 6:** DocumentProcessingJob con reintentos automáticos
- [x] **FASE 6:** TextExtractionService para archivos de texto
- [x] **FASE 6:** Encolado automático después de upload
- [x] **FASE 7:** IChunkingService implementado
- [x] **FASE 7:** IEmbeddingService con Semantic Kernel
- [x] **FASE 7:** IVectorStoreService con Qdrant
- [x] **FASE 7:** DocumentVectorizationJob implementado

### En Progreso
- [ ] **FASE 6:** Extracción de PDF (biblioteca pendiente)
- [ ] **FASE 6:** Extracción de Office (biblioteca pendiente)
- [ ] **FASE 6:** OCR con Azure Computer Vision

### Pendiente
- [ ] Flujo completo de vectorización end-to-end
- [ ] Chat RAG con Semantic Kernel
- [ ] UI de chat conectada

### Tests
- **Total:** 194 tests (27 domain + 84 application + 83 infrastructure)

---

## Fases de Desarrollo

## FASE 1: Infraestructura y Base de Datos ✅ COMPLETADA
**Objetivo:** Configurar toda la infraestructura necesaria para el desarrollo

### 1.1 Docker Compose
- [x] Crear `docker-compose.yml` con:
  - SQL Server 2022 (puerto 1433)
  - Qdrant (puertos 6333, 6334)
  - Azure Computer Vision (puerto 5000)
- [x] Crear `docker-compose.override.yml` para desarrollo local
- [x] Documentar comandos de inicio/parada (QUICKSTART.md)

### 1.2 Configuración de Base de Datos
- [x] Crear script de inicialización SQL Server
- [x] Actualizar migración EF Core con campos faltantes:
  - DocumentChunk (DbSet y configuración)
  - Índices para búsquedas frecuentes
- [x] Configurar Hangfire database (schema en script SQL)
- [ ] Crear seed data para desarrollo (pospuesto a Fase 3)

### 1.3 Configuración de Qdrant
- [x] Script/código para crear colección automáticamente (QdrantInitializationService)
- [x] Definir dimensiones del vector (1536 para OpenAI ada-002)
- [x] Configurar payload schema para filtrado por proyecto (projectId, documentId)

### 1.4 Configuración de Aplicación
- [x] Completar `appsettings.json` con todas las secciones
- [x] Crear `appsettings.Development.json`
- [x] Documentar `appsettings.local.json` (secrets) - ver .env.example
- [x] Validar configuración al inicio de la aplicación (ConfigurationValidator)

**Entregables:**
- [x] docker-compose funcional
- [x] Base de datos inicializada con migraciones
- [x] Qdrant con colección creada (auto)
- [x] Aplicación arranca sin errores

---

## FASE 2: Patrón CQRS con MediatR ✅ COMPLETADA
**Objetivo:** Establecer la arquitectura base para Commands y Queries

### 2.1 Configuración de MediatR
- [x] Instalar paquetes MediatR en Application e Infrastructure
- [x] Configurar pipeline behaviors:
  - ValidationBehavior (FluentValidation)
  - LoggingBehavior
  - ExceptionHandlingBehavior
- [x] Crear interfaces base: ICommand<TResult>, IQuery<TResult>

### 2.2 Patrón Result
- [x] Implementar `Result<T>` para manejo de errores sin excepciones
- [x] Implementar `Error` record para errores tipados
- [x] Crear errores de dominio comunes (NotFound, ValidationError, etc.)

### 2.3 Repositorios Base
- [x] Crear `IRepository<T>` genérico
- [x] Crear `IUnitOfWork` para transacciones
- [x] Implementar `EfRepository<T>` base
- [x] Crear `IProjectRepository` con métodos específicos
- [x] Crear `IDocumentRepository` con métodos específicos

**Entregables:**
- [x] MediatR configurado con pipeline
- [x] Patrón Result implementado
- [x] Repositorios base listos para usar

**Tests:**
- [x] Tests unitarios de Result<T>
- [x] Tests de pipeline behaviors

---

## FASE 3: Gestión de Proyectos (CRUD) ✅ COMPLETADA
**Objetivo:** Implementar el primer caso de uso completo end-to-end

### 3.1 Commands de Proyecto
- [x] `CreateProjectCommand` + Handler + Validator
- [x] `UpdateProjectCommand` + Handler + Validator
- [x] `DeleteProjectCommand` + Handler

### 3.2 Queries de Proyecto
- [x] `GetProjectsQuery` (listado con conteo de documentos)
- [x] `GetProjectByIdQuery` (detalle con conteo de documentos)
- [x] `SearchProjectsQuery` (búsqueda por nombre)

### 3.3 API Endpoints
- [x] `ProjectsController` con endpoints REST:
  - `GET /api/projects` (listado)
  - `GET /api/projects/{id}` (detalle)
  - `GET /api/projects/search?term=x` (búsqueda)
  - `POST /api/projects` (crear)
  - `PUT /api/projects/{id}` (actualizar)
  - `DELETE /api/projects/{id}` (eliminar)
- [x] Documentar endpoints en Swagger

### 3.4 Integración WebUI
- [x] Conectar `Index.razor` con datos reales via MediatR
- [x] Implementar modal de crear/editar proyecto
- [x] Implementar confirmación de borrado
- [x] Navegación a detalle de proyecto (`Projects/Detail.razor`)

**Entregables:**
- [x] CRUD completo de proyectos funcionando
- [x] UI conectada a datos reales
- [x] Sin MockDataService para proyectos

**Tests:**
- [x] Tests unitarios de Commands/Queries handlers (30 tests nuevos)
- [ ] Tests de integración de repositorio (pendiente para Fase posterior)
- [ ] Tests de API endpoints (pendiente para Fase posterior)

---

## FASE 4: Almacenamiento de Archivos (NAS) ✅ COMPLETADA
**Objetivo:** Implementar el servicio de almacenamiento con abstracción para futuros proveedores

### 4.1 Abstracción de Storage
- [x] Crear `IStorageService` interface:
  ```csharp
  Task<string> SaveFileAsync(Stream content, string fileName, Guid projectId);
  Task<Stream> GetFileAsync(string filePath);
  Task DeleteFileAsync(string filePath);
  Task<bool> ExistsAsync(string filePath);
  Task<string> GetFileHashAsync(string filePath);
  Task<long> GetFileSizeAsync(string filePath);
  Task DeleteProjectFilesAsync(Guid projectId);
  ```
- [x] Usar `FileStorageOptions` existente para configuración

### 4.2 Implementación Local/NAS
- [x] Implementar `LocalFileStorageService`
- [x] Estructura de carpetas: `{basePath}/{projectId}/{fileName}`
- [x] Manejo de nombres duplicados (añadir sufijo timestamp)
- [x] Cálculo de hash SHA256 para deduplicación
- [x] Validación de tipos de archivo permitidos
- [x] Límite de tamaño configurable
- [x] Sanitización de nombres de archivo (prevención path traversal)

### 4.3 Configuración
- [x] Sección `FileStorage` en appsettings (ya existía)
- [x] Registro de `IStorageService` en DI

**Entregables:**
- [x] Servicio de almacenamiento funcional
- [x] Archivos se guardan en sistema de archivos
- [x] Preparado para añadir Azure Blob, S3, etc.

**Tests:**
- [x] 15 tests de integración con archivos reales temporales
- [x] Tests de path traversal, duplicados, extensiones, hash, etc.

---

## FASE 5: Subida de Documentos ✅ COMPLETADA
**Objetivo:** Implementar la carga de documentos a proyectos

### 5.1 Commands de Documento
- [x] `UploadDocumentCommand` + Handler:
  - Recibe: ProjectId, Stream, FileName
  - Guarda archivo en storage
  - Crea registro en BD
  - Calcula hash para deduplicación
  - Detecta tipo de archivo
  - Encola procesamiento automático
- [x] `DeleteDocumentCommand` + Handler (elimina BD + storage)
- [ ] `UploadBatchDocumentsCommand` para carga múltiple (pendiente)

### 5.2 Queries de Documento
- [x] `GetDocumentsByProjectQuery` (listado por proyecto)
- [x] `GetDocumentByIdQuery` (detalle)
- [x] `GetDocumentContentQuery` (descarga stream)

### 5.3 API Endpoints
- [x] `DocumentsController`:
  - `POST /api/projects/{projectId}/documents` (upload)
  - `GET /api/projects/{projectId}/documents` (listado)
  - `GET /api/documents/{id}` (detalle)
  - `GET /api/documents/{id}/download` (descarga)
  - `DELETE /api/documents/{id}` (eliminar)
- [ ] Batch upload (pendiente)

### 5.4 Detección de Tipo de Archivo
- [x] Implementar `IFileTypeDetector`:
  - Por extensión: .txt, .md → TextBased
  - Por extensión: .jpg, .png, .tiff → ImageBased
  - Por extensión: .pdf → Pdf
  - Por extensión: .docx, .xlsx → OfficeDocument
- [x] Marcar documentos que requieren OCR

### 5.5 Integración WebUI
- [x] Conectar `FileUpload.razor` con MediatR (subida real)
- [x] Mostrar progreso de carga por archivo
- [x] Actualizar lista de documentos después de carga
- [x] Eliminar documentos desde la tabla

**Entregables:**
- [x] Subida de archivos funcionando (API)
- [x] Archivos almacenados en NAS
- [x] Metadatos en SQL Server
- [x] UI conectada con funcionalidad completa

**Tests:**
- [x] Tests de UploadDocumentCommand (5 tests)
- [x] Tests de detección de tipo de archivo (17 tests)
- [x] Tests de GetDocumentsByProjectQuery (4 tests)
- [x] Tests de GetDocumentByIdQuery (3 tests)
- [x] Tests de DeleteDocumentCommand (4 tests)

---

## FASE 6: Procesamiento Asíncrono con Hangfire ✅ COMPLETADA (parcial)
**Objetivo:** Configurar jobs en segundo plano para procesamiento de documentos

### 6.1 Configuración de Hangfire
- [x] Instalar paquetes Hangfire (Core, SqlServer, AspNetCore)
- [x] Configurar storage en SQL Server (schema HangFire)
- [x] Configurar dashboard en `/hangfire`
- [x] Configurar workers y colas (default, documents)
- [x] HangfireOptions para configuración
- [x] **Método centralizado `AddHangfireServices()` en DependencyInjection**
- [x] **Hangfire habilitado en WebUI (antes faltaba)**
- [x] **`IDocumentProcessingQueue` requerido (no nullable)**

### 6.2 Job de Extracción de Texto
- [x] `ITextExtractionService` interface
- [x] `TextExtractionService` implementation (texto básico)
- [x] `DocumentProcessingJob` con reintentos automáticos
- [x] `IDocumentProcessingQueue` para encolar jobs
- [x] `DocumentProcessingQueue` implementación con Hangfire
- [x] `InMemoryDocumentProcessingQueue` fallback cuando no hay Hangfire
- [x] Integración con `UploadDocumentCommand` (encola después de upload)
- [ ] **Extracción de PDF con PdfPig** (pendiente)
- [ ] **Extracción de Office con OpenXml** (pendiente)

### 6.3 Job de OCR
- [ ] `OcrProcessingJob`:
  - Enviar imagen a Azure Computer Vision
  - Recibir texto extraído
  - Guardar en Document.Content
  - Marcar como PendingReview
- [ ] Implementar reintentos con backoff exponencial
- [ ] Manejar timeout de servicio OCR

### 6.4 Flujo de Procesamiento
- [x] Después de upload → encolar TextExtractionJob
- [x] Actualizar estado del documento (Pending → Processing → Completed/Failed)
- [x] Encolar vectorización después de extracción exitosa
- [ ] Si requiere OCR → encolar OcrProcessingJob
- [ ] Notificar a UI cuando complete (SignalR opcional)

### 6.5 Revisión de OCR
- [ ] `GetPendingOcrReviewQuery`
- [ ] `ApproveOcrResultCommand`
- [ ] `UpdateOcrContentCommand` (edición manual)
- [ ] UI para revisar y aprobar resultados OCR

**Entregables:**
- [x] Hangfire dashboard accesible (API y WebUI)
- [x] Jobs procesando documentos de texto automáticamente
- [x] Estado de documentos actualizado correctamente
- [ ] Extracción de PDF y Office
- [ ] Interfaz de revisión de OCR

**Tests:**
- [x] Tests de DocumentProcessingJob (mock de servicios externos)
- [ ] Tests de integración de flujo completo

---

## FASE 7: Vectorización y Qdrant ⏳ EN PROGRESO
**Objetivo:** Implementar chunking, embeddings y almacenamiento vectorial

### 7.1 Servicio de Chunking
- [x] Crear `IChunkingService`:
  - Dividir texto en chunks de tamaño configurable
  - Overlap configurable entre chunks
  - Preservar contexto (no cortar mid-sentence)
- [x] `ChunkingOptions` para configuración
- [x] Crear DocumentChunk por cada fragmento
- [x] Almacenar posición (StartCharIndex, EndCharIndex)

### 7.2 Servicio de Embeddings
- [x] Crear `IEmbeddingService` interface
- [x] Implementar `SemanticKernelEmbeddingService`:
  - Soporte para Ollama (nomic-embed-text)
  - Soporte para OpenAI (text-embedding-ada-002)
- [x] Configuración de proveedor en appsettings

### 7.3 Servicio de Vector Store
- [x] Crear `IVectorStoreService` interface
- [x] Implementar `QdrantVectorStoreService`:
  - Guardar chunks con embeddings
  - Buscar por similitud
  - Filtrar por projectId
  - Eliminar por documentId

### 7.4 Job de Vectorización
- [x] `DocumentVectorizationJob`:
  - Chunking del documento
  - Generar embedding por chunk
  - Guardar en Qdrant con payload
  - Actualizar QdrantPointId en DocumentChunk
  - Marcar documento como IsVectorized

### 7.5 Flujo Completo (Pendiente verificación)
- [x] Después de extracción → encolar VectorizationJob
- [ ] Solo vectorizar documentos con Content no vacío
- [ ] Manejar re-vectorización si contenido cambia
- [ ] **Verificar flujo end-to-end funciona**

**Entregables:**
- [x] Servicios de chunking, embedding y vector store implementados
- [x] Job de vectorización implementado
- [ ] Verificar flujo completo funciona
- [ ] Tests de integración

**Tests:**
- [x] Tests de ChunkingService (10 tests)
- [x] Tests de QdrantVectorStoreService (9 tests)
- [ ] Tests de integración con Qdrant real

---

## FASE 8: Borrado de Documentos ✅ COMPLETADA (básico)
**Objetivo:** Implementar borrado completo (BD, NAS, Qdrant)

### 8.1 Command de Borrado
- [x] `DeleteDocumentCommand`:
  - Eliminar archivo de NAS
  - Eliminar Document de BD (cascade elimina chunks)
  - **Pendiente:** Eliminar puntos de Qdrant (por documentId)

### 8.2 Borrado en Cascada de Proyecto
- [x] `DeleteProjectCommand`:
  - Borrar archivos del proyecto en NAS
  - Borrar proyecto (cascade borra documentos)
  - **Pendiente:** Borrar puntos de Qdrant del proyecto

### 8.3 API Endpoint
- [x] `DELETE /api/documents/{id}`
- [x] Confirmar borrado en UI

### 8.4 Manejo de Errores
- [ ] Si falla borrado de NAS → log y continuar
- [ ] Si falla Qdrant → reintentar o marcar para limpieza posterior
- [ ] Job de limpieza de huérfanos (opcional)

**Entregables:**
- [x] Borrado de BD y NAS funcionando
- [ ] Borrado de Qdrant
- [ ] Sin datos huérfanos en ningún sistema

**Tests:**
- [x] Tests de DeleteDocumentCommand (4 tests)
- [ ] Tests de cascada con Qdrant

---

## FASE 9: Configuración de Asistentes IA
**Objetivo:** Configurar Semantic Kernel para chat RAG

### 9.1 Configuración de Semantic Kernel
- [x] Instalar paquetes Semantic Kernel
- [x] Configurar múltiples proveedores (en SemanticKernelOptions):
  - Ollama (modelos locales)
  - OpenAI (GPT-4)
  - Google Gemini
- [ ] Selector de proveedor en runtime

### 9.2 Servicio de Chat
- [ ] Crear `IChatService`:
  ```csharp
  Task<ChatResponse> ChatAsync(Guid projectId, string message, CancellationToken ct);
  IAsyncEnumerable<string> ChatStreamAsync(Guid projectId, string message, CancellationToken ct);
  ```

### 9.3 Implementación RAG
- [ ] Búsqueda de contexto relevante:
  1. Convertir pregunta a embedding
  2. Buscar en Qdrant (filtrado por projectId)
  3. Recuperar chunks relevantes (top K)
  4. Obtener contenido de BD
- [ ] Construir prompt con contexto
- [ ] Llamar a LLM
- [ ] Retornar respuesta + referencias a documentos

### 9.4 Prompt Engineering
- [ ] Crear template de prompt RAG
- [ ] Instrucciones para citar fuentes
- [ ] Manejo de contexto insuficiente
- [ ] Límite de tokens de contexto

**Entregables:**
- Chat funcionando con RAG
- Respuestas con referencias a documentos
- Soporte para múltiples proveedores de LLM

**Tests:**
- Tests de búsqueda de contexto
- Tests de construcción de prompt
- Tests de integración con LLM (mock)

---

## FASE 10: Chat en WebUI
**Objetivo:** Integrar el chat completo en la interfaz de usuario

### 10.1 API de Chat
- [ ] `ChatController`:
  - `POST /api/projects/{projectId}/chat` (mensaje simple)
  - `POST /api/projects/{projectId}/chat/stream` (streaming)
- [ ] Retornar ChatResponseDto con referencias

### 10.2 Integración WebUI
- [ ] Conectar `ChatWindow.razor` con API real
- [ ] Implementar streaming de respuestas
- [ ] Mostrar referencias como enlaces clicables
- [ ] Historial de conversación (sesión)
- [ ] Indicador de "escribiendo..."

### 10.3 Vista de Documento Referenciado
- [ ] Modal o panel para ver documento
- [ ] Resaltar fragmento relevante
- [ ] Navegación entre referencias

### 10.4 Configuración de Chat
- [ ] Selector de modelo LLM
- [ ] Ajuste de temperatura
- [ ] Número de documentos de contexto

**Entregables:**
- Chat RAG completo en UI
- Respuestas en streaming
- Referencias navegables
- Configuración de modelo

**Tests:**
- Tests de componentes Blazor (bUnit)
- Tests end-to-end de flujo de chat

---

## FASE 11: Pulido y Optimización
**Objetivo:** Mejorar rendimiento, UX y preparar para producción

### 11.1 Optimización de Rendimiento
- [ ] Caché de embeddings frecuentes
- [ ] Paginación eficiente en listados
- [ ] Índices de BD optimizados
- [ ] Compresión de respuestas API

### 11.2 Mejoras de UX
- [ ] Notificaciones de progreso (SignalR)
- [ ] Búsqueda global de documentos
- [ ] Filtros avanzados en listados
- [ ] Exportación de conversaciones

### 11.3 Logging y Monitoreo
- [ ] Logging estructurado con Serilog
- [ ] Métricas de uso
- [ ] Dashboard de monitoreo
- [ ] Alertas de errores

### 11.4 Seguridad
- [ ] Validación de entrada exhaustiva
- [ ] Rate limiting en API
- [ ] Sanitización de contenido
- [ ] Auditoría de acciones

### 11.5 Documentación
- [ ] README actualizado
- [ ] Guía de instalación
- [ ] Documentación de API (OpenAPI)
- [ ] Guía de usuario

**Entregables:**
- Aplicación optimizada
- Documentación completa
- Lista para despliegue

---

## Resumen de Fases y Dependencias

```
┌─────────────────────────────────────────────────────────────────┐
│ FASE 1: Infraestructura ✅                                       │
│ Docker, BD, Qdrant, Config                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 2: CQRS con MediatR ✅                                      │
│ Pipeline, Result, Repositorios                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 3: Gestión de Proyectos ✅                                  │
│ CRUD completo, API, UI                                          │
└─────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┴───────────────────┐
          ▼                                       ▼
┌─────────────────────────┐         ┌─────────────────────────────┐
│ FASE 4: Storage ✅      │         │ FASE 6: Hangfire ✅          │
│ NAS, Local              │         │ Jobs asíncronos             │
└─────────────────────────┘         └─────────────────────────────┘
          │                                       │
          ▼                                       │
┌─────────────────────────┐                       │
│ FASE 5: Upload Docs ✅  │◄──────────────────────┘
│ Carga, Detección tipo   │
└─────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 7: Vectorización ⏳                                         │
│ Chunking, Embeddings, Qdrant                                    │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 8: Borrado de Documentos ✅ (básico)                        │
│ BD + NAS (+ Qdrant pendiente)                                   │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 9: Asistentes IA                                           │
│ Semantic Kernel, RAG                                            │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 10: Chat en WebUI                                          │
│ Streaming, Referencias                                          │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 11: Pulido y Optimización                                  │
│ Performance, UX, Docs                                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Estimación de Tareas por Fase

| Fase | Descripción | Complejidad | Tests | Estado |
|------|-------------|-------------|-------|--------|
| 1 | Infraestructura | Media | Mínimos | ✅ |
| 2 | CQRS/MediatR | Media | 31 | ✅ |
| 3 | Proyectos CRUD | Media | 30 | ✅ |
| 4 | Storage NAS | Baja | 15 | ✅ |
| 5 | Upload Docs | Media | 33 | ✅ |
| 6 | Hangfire | Alta | ~5 | ✅ (parcial) |
| 7 | Vectorización | Alta | 19 | ⏳ En progreso |
| 8 | Borrado | Media | 4 | ✅ (básico) |
| 9 | IA/RAG | Alta | ~15 | Pendiente |
| 10 | Chat UI | Media | ~10 | Pendiente |
| 11 | Pulido | Variable | ~10 | Pendiente |

**Tests actuales:** 194 (27 domain + 84 application + 83 infrastructure)
**Total estimado adicional:** ~50 tests más

---

## Próximos Pasos Inmediatos

1. **Verificar flujo completo de procesamiento:**
   - Subir archivo .txt desde WebUI
   - Verificar que aparece job en Hangfire dashboard
   - Verificar que estado cambia a Completed
   - Verificar que contenido se extrae

2. **Implementar extracción de PDF:**
   - Agregar paquete PdfPig
   - Implementar `ExtractFromPdfAsync`

3. **Implementar extracción de Office:**
   - Agregar paquete DocumentFormat.OpenXml
   - Implementar `ExtractFromOfficeDocumentAsync`

4. **Verificar vectorización:**
   - Asegurar que embeddings se generan
   - Asegurar que se guardan en Qdrant

---

## Criterios de Aceptación por Fase

Cada fase se considera completa cuando:

1. ✅ Todos los items del checklist están marcados
2. ✅ Tests escritos y pasando
3. ✅ Código revisado (sin TODOs críticos)
4. ✅ Documentación actualizada
5. ✅ Funcionalidad verificada manualmente
6. ✅ Commit con mensaje descriptivo

---

## Notas de Implementación

### Prioridades
1. **Funcionalidad sobre perfección:** Implementar primero, optimizar después
2. **Tests antes de refactor:** No refactorizar sin cobertura de tests
3. **Incrementos pequeños:** Commits frecuentes con cambios atómicos

### Convenciones
- Un Command/Query por archivo
- Validators junto a Commands
- DTOs en Application/DTOs
- Servicios externos en Infrastructure/Services

### Riesgos Identificados
- **OCR Azure:** Puede tener latencia alta o fallar
- **Qdrant:** Requiere dimensiones correctas del modelo
- **LLM:** Costos de API, límites de tokens
- **Archivos grandes:** Pueden agotar memoria

### Mitigaciones
- Circuit breaker para servicios externos
- Streaming para archivos grandes
- Caché de embeddings
- Límites configurables

---

## Cambios Recientes (2026-01-26)

### Bugfix: Procesamiento de Documentos
- **Problema:** Documentos no se procesaban después de subirse
- **Causa:** WebUI no tenía Hangfire configurado
- **Solución:**
  - Método `AddHangfireServices()` centralizado en `DependencyInjection.cs`
  - Hangfire habilitado en WebUI
  - `IDocumentProcessingQueue` ahora es requerido (no nullable)
  - Test actualizado para incluir mock del processing queue

---

*Documento creado: 2025-01-25*
*Última actualización: 2026-01-26 - Bugfix Hangfire en WebUI*
