# Plan de Desarrollo - Babel

## Visión General

Este documento define el roadmap completo para desarrollar Babel, un sistema de gestión documental con capacidades de IA y OCR. El desarrollo sigue un enfoque incremental con casos de uso bien definidos.

---

## Estado Actual (Enero 2025)

### Completado
- [x] Estructura Clean Architecture (Domain, Application, Infrastructure, API, WebUI)
- [x] Entidades de dominio: Project, Document, DocumentChunk
- [x] DbContext con EF Core 10.0 y migración inicial
- [x] Health checks (SQL Server, Qdrant, Azure OCR)
- [x] Layout WebUI con Blazor Server + MudBlazor 8.0
- [x] Componentes UI básicos (ProjectCard, ChatWindow, FileUpload)
- [x] 160 tests unitarios (27 domain + 77 application + 56 infrastructure)
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

### En Progreso
- [x] **FASE 6:** Hangfire configurado (dashboard, workers, jobs básicos)
- [ ] **FASE 6:** Extracción de PDF y Office (pendiente bibliotecas)

### Pendiente
- [ ] Servicios de IA (Semantic Kernel)
- [ ] OCR con Azure Computer Vision
- [ ] Vectorización con Qdrant

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

## FASE 6: Procesamiento Asíncrono con Hangfire ⏳ EN PROGRESO
**Objetivo:** Configurar jobs en segundo plano para procesamiento de documentos

### 6.1 Configuración de Hangfire
- [x] Instalar paquetes Hangfire (Core, SqlServer, AspNetCore)
- [x] Configurar storage en SQL Server (schema HangFire)
- [x] Configurar dashboard en `/hangfire`
- [x] Configurar workers y colas (default, documents)
- [x] HangfireOptions para configuración

### 6.2 Job de Extracción de Texto
- [x] `ITextExtractionService` interface
- [x] `TextExtractionService` implementation (texto básico)
- [x] `DocumentProcessingJob` con reintentos automáticos
- [x] `IDocumentProcessingQueue` para encolar jobs
- [x] Integración con `UploadDocumentCommand` (encola después de upload)
- [ ] Extracción de PDF con biblioteca (pendiente)
- [ ] Extracción de Office con biblioteca (pendiente)

### 6.3 Job de OCR
- [ ] `OcrProcessingJob`:
  - Enviar imagen a Azure Computer Vision
  - Recibir texto extraído
  - Guardar en Document.Content
  - Marcar como PendingReview
- [ ] Implementar reintentos con backoff exponencial
- [ ] Manejar timeout de servicio OCR

### 6.4 Flujo de Procesamiento
- [ ] Después de upload → encolar TextExtractionJob
- [ ] Si requiere OCR → encolar OcrProcessingJob
- [ ] Actualizar estado del documento en cada paso
- [ ] Notificar a UI cuando complete (SignalR opcional)

### 6.5 Revisión de OCR
- [ ] `GetPendingOcrReviewQuery`
- [ ] `ApproveOcrResultCommand`
- [ ] `UpdateOcrContentCommand` (edición manual)
- [ ] UI para revisar y aprobar resultados OCR

**Entregables:**
- Hangfire dashboard accesible
- Jobs procesando documentos automáticamente
- Estado de documentos actualizado en tiempo real
- Interfaz de revisión de OCR

**Tests:**
- Tests unitarios de jobs (mock de servicios externos)
- Tests de integración de flujo completo

---

## FASE 7: Vectorización y Qdrant
**Objetivo:** Implementar chunking, embeddings y almacenamiento vectorial

### 7.1 Servicio de Chunking
- [ ] Crear `IChunkingService`:
  - Dividir texto en chunks de tamaño configurable
  - Overlap configurable entre chunks
  - Preservar contexto (no cortar mid-sentence)
- [ ] Crear DocumentChunk por cada fragmento
- [ ] Almacenar posición (StartCharIndex, EndCharIndex)

### 7.2 Servicio de Embeddings
- [ ] Crear `IEmbeddingService` interface
- [ ] Implementar con Semantic Kernel:
  - Soporte para Ollama (nomic-embed-text)
  - Soporte para OpenAI (text-embedding-ada-002)
  - Soporte para Gemini
- [ ] Configuración de proveedor en appsettings

### 7.3 Job de Vectorización
- [ ] `DocumentVectorizationJob`:
  - Chunking del documento
  - Generar embedding por chunk
  - Guardar en Qdrant con payload:
    ```json
    {
      "documentId": "guid",
      "projectId": "guid",
      "chunkIndex": 0,
      "fileName": "documento.pdf"
    }
    ```
  - Actualizar QdrantPointId en DocumentChunk
  - Marcar documento como IsVectorized

### 7.4 Flujo Completo
- [ ] Después de extracción/OCR → encolar VectorizationJob
- [ ] Solo vectorizar documentos con Content no vacío
- [ ] Manejar re-vectorización si contenido cambia

**Entregables:**
- Documentos divididos en chunks
- Embeddings almacenados en Qdrant
- Búsqueda vectorial funcional

**Tests:**
- Tests de chunking service
- Tests de integración con Qdrant

---

## FASE 8: Borrado de Documentos
**Objetivo:** Implementar borrado completo (BD, NAS, Qdrant)

### 8.1 Command de Borrado
- [ ] `DeleteDocumentCommand`:
  - Eliminar archivo de NAS
  - Eliminar puntos de Qdrant (por documentId)
  - Eliminar DocumentChunks de BD
  - Eliminar Document de BD
  - Todo en una transacción

### 8.2 Borrado en Cascada de Proyecto
- [ ] `DeleteProjectCommand` actualizado:
  - Borrar todos los documentos del proyecto
  - Borrar proyecto

### 8.3 API Endpoint
- [ ] `DELETE /api/documents/{id}`
- [ ] Confirmar borrado en UI

### 8.4 Manejo de Errores
- [ ] Si falla borrado de NAS → log y continuar
- [ ] Si falla Qdrant → reintentar o marcar para limpieza posterior
- [ ] Job de limpieza de huérfanos (opcional)

**Entregables:**
- Borrado completo funcionando
- Sin datos huérfanos en ningún sistema
- Confirmación en UI

**Tests:**
- Tests de DeleteDocumentCommand
- Tests de cascada en DeleteProjectCommand

---

## FASE 9: Configuración de Asistentes IA
**Objetivo:** Configurar Semantic Kernel para chat RAG

### 9.1 Configuración de Semantic Kernel
- [ ] Instalar paquetes Semantic Kernel
- [ ] Configurar múltiples proveedores:
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
│ FASE 1: Infraestructura                                         │
│ Docker, BD, Qdrant, Config                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 2: CQRS con MediatR                                        │
│ Pipeline, Result, Repositorios                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 3: Gestión de Proyectos                                    │
│ CRUD completo, API, UI                                          │
└─────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┴───────────────────┐
          ▼                                       ▼
┌─────────────────────────┐         ┌─────────────────────────────┐
│ FASE 4: Storage (NAS)   │         │ FASE 6: Hangfire            │
│ Abstracción, Local      │         │ Jobs asíncronos             │
└─────────────────────────┘         └─────────────────────────────┘
          │                                       │
          ▼                                       │
┌─────────────────────────┐                       │
│ FASE 5: Upload Docs     │◄──────────────────────┘
│ Carga, Detección tipo   │
└─────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 7: Vectorización                                           │
│ Chunking, Embeddings, Qdrant                                    │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ FASE 8: Borrado de Documentos                                   │
│ BD + NAS + Qdrant                                               │
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
| 5 | Upload Docs | Media | 57 | ✅ |
| 6 | Hangfire | Alta | ~10 | Pendiente |
| 7 | Vectorización | Alta | ~15 | Pendiente |
| 8 | Borrado | Media | ~10 | Pendiente |
| 9 | IA/RAG | Alta | ~15 | Pendiente |
| 10 | Chat UI | Media | ~10 | Pendiente |
| 11 | Pulido | Variable | ~10 | Pendiente |

**Tests actuales:** 103 (27 domain + 61 application + 15 infrastructure)
**Total estimado adicional:** ~80 tests más

---

## Criterios de Aceptación por Fase

Cada fase se considera completa cuando:

1. ✅ Todos los items del checklist están marcados
2. ✅ Tests escritos y pasando
3. ✅ Código revisado (sin TODOs pendientes)
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

*Documento creado: 2026-01-25*
*Última actualización: 2026-01-25 - Fase 4 completada*
