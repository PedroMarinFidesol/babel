# Babel - Sistema de Gestión Documental con IA y OCR

Aplicación de gestión documental inteligente con capacidades de OCR, búsqueda semántica y procesamiento mediante modelos de lenguaje (LLM).

## Índice

- [Descripción General](#descripción-general)
- [Stack Tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Requisitos Previos](#requisitos-previos)
- [Configuración del Entorno de Desarrollo](#configuración-del-entorno-de-desarrollo)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Flujo de Trabajo](#flujo-de-trabajo)
- [Comandos de Desarrollo](#comandos-de-desarrollo)

## Descripción General

Babel es una aplicación monolítica modular desarrollada en .NET Core que permite:

- **Gestión de documentos**: Carga, almacenamiento y organización de documentos
- **OCR (Reconocimiento Óptico de Caracteres)**: Extracción de texto de imágenes mediante Azure Computer Vision (local)
- **Vectorización y búsqueda semántica**: Almacenamiento de embeddings en Qdrant para búsquedas inteligentes
- **Procesamiento con IA**: Integración con múltiples LLMs mediante Semantic Kernel (Ollama, OpenAI, Gemini)
- **Procesamiento asíncrono**: Jobs en background con Hangfire

## Stack Tecnológico

### Backend
- **.NET 8.0** (o superior)
- **ASP.NET Core** para APIs y Web
- **Blazor Server** para UI

### Bases de Datos
- **SQL Server** (Docker): Base de datos relacional principal
- **Qdrant** (Docker) : Base de datos vectorial para embeddings y búsqueda semántica

### IA y OCR
- **Semantic Kernel**: Framework para orquestación de LLMs
- **Azure Computer Vision** (Docker): Servicio OCR
- **Proveedores LLM**:
  - Ollama (local)
  - OpenAI API
  - Google Gemini API

### Infraestructura
- **Hangfire**: Procesamiento de jobs en background
- **Docker**: Contenedorización de servicios (SQL Server, Qdrant)
- **Sistema de archivos local/NAS**: Almacenamiento de documentos físicos

### Patrón Arquitectónico
- **Clean Architecture**: Separación en capas (Domain, Application, Infrastructure, Presentation)
- **Monolito Modular**: Todo en una solución, separado lógicamente

## Arquitectura

### Clean Architecture - Capas

```
┌─────────────────────────────────────────────────────────┐
│                   Babel.API / Babel.WebUI               │
│                   (Presentation Layer)                   │
│              Blazor Server + REST API Endpoints          │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                  Babel.Application                       │
│                  (Application Layer)                     │
│     Use Cases, DTOs, Command/Query Handlers (MediatR)   │
│           Interfaces de servicios externos               │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                   Babel.Domain                           │
│                   (Domain Layer)                         │
│         Entidades, Value Objects, Domain Events          │
│              Lógica de negocio pura                      │
└──────────────────────────────────────────────────────────┘
                            ▲
┌───────────────────────────┴─────────────────────────────┐
│                Babel.Infrastructure                      │
│                (Infrastructure Layer)                    │
│    - Repositorios (SQL Server, Qdrant)                  │
│    - Servicios OCR (Azure Computer Vision)              │
│    - Integración LLM (Semantic Kernel)                  │
│    - File Storage (Sistema de archivos)                 │
│    - Jobs (Hangfire)                                    │
└──────────────────────────────────────────────────────────┘
```

### Módulos Principales

1. **Documents Module**: Gestión de documentos (CRUD, metadatos)
2. **OCR Module**: Procesamiento de imágenes y extracción de texto
3. **Vectorization Module**: Generación de embeddings y almacenamiento en Qdrant
4. **Search Module**: Búsqueda semántica y tradicional
5. **AI Module**: Chat, resúmenes, análisis de documentos con LLMs
6. **Jobs Module**: Procesamiento asíncrono (OCR batch, vectorización)

## Requisitos Previos

- Los documentos se agruparán en proyectos
- Los proyecto tendrán un nombre y una id
- Se podrán subir los archivos por lotes
- Dependiendo de la extensión se procesarán directamente en la BBDD o se pasarán al módulo de OCR
- Cuando un archivo se procesa un archivo mediante ocr se podrá revisar par su edición
- En la pantalla principal aparecerán los proyecto en fichas indicando el nombre y el numero de archivos
- Al seleccionar un proyecto aparecerá:  
    1- una ventana de chat
    2- un listado de archivos
    3- un formulario de subida de archivos
- Las respuestas de la ventana de chat con el llm devolverá la respuesta junto con un listado de archivos referenciados en la consulta.
- En la bbdd vectorial se guardarán las referencias a los archivos


### Software Requerido

- **.NET SDK 8.0+**: [Descargar](https://dotnet.microsoft.com/download)
- **Docker Desktop**: [Descargar](https://www.docker.com/products/docker-desktop)
- **Visual Studio 2022** o **Visual Studio Code**
- **Git**

### Servicios Opcionales (según configuración)

- **Ollama**: [Instalar](https://ollama.ai/) (para LLMs locales)
- **OpenAI API Key**: [Obtener](https://platform.openai.com/)
- **Google Gemini API Key**: [Obtener](https://ai.google.dev/)
- **Azure Computer Vision**: Endpoint y clave (o instalación local)

## Configuración del Entorno de Desarrollo

### 1. Clonar el Repositorio

```bash
git clone https://github.com/tu-usuario/babel.git
cd babel
```

### 2. Iniciar Servicios con Docker

Crear archivo `docker-compose.yml` en la raíz:

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: babel-sqlserver
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

  qdrant:
    image: qdrant/qdrant:latest
    container_name: babel-qdrant
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant-data:/qdrant/storage

volumes:
  sqlserver-data:
  qdrant-data:
```

Iniciar servicios:

```bash
docker-compose up -d
```

### 3. Configurar appsettings.json

En `Babel.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BabelDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;",
    "HangfireConnection": "Server=localhost,1433;Database=BabelHangfire;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333",
    "CollectionName": "documents"
  },
  "AzureComputerVision": {
    "Endpoint": "http://localhost:5000",
    "ApiKey": "your-local-key"
  },
  "SemanticKernel": {
    "DefaultProvider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelName": "llama2"
    },
    "OpenAI": {
      "ApiKey": "",
      "ModelName": "gpt-4"
    },
    "Gemini": {
      "ApiKey": "",
      "ModelName": "gemini-pro"
    }
  },
  "FileStorage": {
    "BasePath": "C:\\BabelFiles",
    "MaxFileSizeMB": 50
  },
  "Hangfire": {
    "DashboardPath": "/hangfire",
    "WorkerCount": 5
  }
}
```

### 4. Aplicar Migraciones de Base de Datos

```bash
cd Babel.Infrastructure
dotnet ef database update --startup-project ../Babel.API
```

### 5. Instalar Ollama (Opcional)

Si usarás LLMs locales:

```bash
# Instalar Ollama desde https://ollama.ai/
ollama pull llama2
ollama pull nomic-embed-text  # Para embeddings
```

### 6. Ejecutar la Aplicación

```bash
cd Babel.API
dotnet run
```

La aplicación estará disponible en:
- **Blazor UI**: https://localhost:5001
- **Hangfire Dashboard**: https://localhost:5001/hangfire
- **Qdrant Dashboard**: http://localhost:6333/dashboard

## Estructura del Proyecto

```
Babel/
├── Babel.Domain/                    # Capa de Dominio
│   ├── Entities/
│   │   ├── Document.cs
│   │   ├── DocumentVersion.cs
│   │   └── DocumentMetadata.cs
│   ├── ValueObjects/
│   ├── Enums/
│   │   └── DocumentStatus.cs
│   └── Interfaces/
│       └── IRepository.cs
│
├── Babel.Application/               # Capa de Aplicación
│   ├── DTOs/
│   ├── Commands/
│   │   ├── UploadDocumentCommand.cs
│   │   └── ProcessOcrCommand.cs
│   ├── Queries/
│   │   ├── SearchDocumentsQuery.cs
│   │   └── GetDocumentQuery.cs
│   ├── Handlers/
│   ├── Interfaces/
│   │   ├── IDocumentService.cs
│   │   ├── IOcrService.cs
│   │   ├── IVectorService.cs
│   │   └── ILlmService.cs
│   └── Validators/
│
├── Babel.Infrastructure/            # Capa de Infraestructura
│   ├── Data/
│   │   ├── BabelDbContext.cs
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── DocumentRepository.cs
│   │   └── QdrantRepository.cs
│   ├── Services/
│   │   ├── OCR/
│   │   │   └── AzureComputerVisionService.cs
│   │   ├── AI/
│   │   │   ├── SemanticKernelService.cs
│   │   │   ├── OllamaProvider.cs
│   │   │   ├── OpenAIProvider.cs
│   │   │   └── GeminiProvider.cs
│   │   ├── Storage/
│   │   │   └── FileSystemStorageService.cs
│   │   └── Vectorization/
│   │       └── QdrantVectorService.cs
│   ├── Jobs/
│   │   ├── OcrProcessingJob.cs
│   │   └── DocumentVectorizationJob.cs
│   └── Configuration/
│       └── DependencyInjection.cs
│
├── Babel.API/                       # Capa de Presentación (API)
│   ├── Controllers/
│   │   ├── DocumentsController.cs
│   │   ├── SearchController.cs
│   │   └── ChatController.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── Babel.WebUI/                     # Capa de Presentación (Blazor)
│   ├── Pages/
│   │   ├── Documents/
│   │   │   ├── Index.razor
│   │   │   ├── Upload.razor
│   │   │   └── Detail.razor
│   │   ├── Search/
│   │   │   └── Index.razor
│   │   └── Chat/
│   │       └── Index.razor
│   ├── Components/
│   ├── Services/
│   └── Program.cs
│
├── docker-compose.yml
└── README.md
```

## Flujo de Trabajo

### 1. Carga de Documento

```
Usuario → Upload → FileSystem Storage → SQL Server (metadata)
                 ↓
            [Hangfire Job]
                 ↓
         OCR Processing → Extract Text
                 ↓
            SQL Server (update content)
                 ↓
         Generate Embeddings (Semantic Kernel)
                 ↓
            Qdrant (store vectors)
```

### 2. Búsqueda Semántica

```
Usuario → Query Text
          ↓
   Generate Query Embedding (Semantic Kernel)
          ↓
   Qdrant Vector Search
          ↓
   Return Document IDs
          ↓
   SQL Server (fetch metadata)
          ↓
   Display Results
```

### 3. Chat con Documentos

```
Usuario → Question
          ↓
   Vector Search (retrieve relevant docs)
          ↓
   Build Context (RAG pattern)
          ↓
   LLM Processing (Semantic Kernel)
          ↓
   Stream Response to UI
```

## Comandos de Desarrollo

### Compilar la Solución

```bash
dotnet build
```

### Ejecutar Tests

```bash
dotnet test
```

### Ejecutar la Aplicación

```bash
cd Babel.API
dotnet run
```

### Aplicar Migraciones

```bash
# Crear migración
dotnet ef migrations add MigrationName --project Babel.Infrastructure --startup-project Babel.API

# Actualizar base de datos
dotnet ef database update --project Babel.Infrastructure --startup-project Babel.API
```

### Limpiar y Recompilar

```bash
dotnet clean
dotnet build
```

### Gestionar Docker

```bash
# Iniciar servicios
docker-compose up -d

# Detener servicios
docker-compose down

# Ver logs
docker-compose logs -f

# Reiniciar un servicio específico
docker-compose restart qdrant
```

### Verificar Estado de Servicios

```bash
# SQL Server
docker exec -it babel-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"

# Qdrant
curl http://localhost:6333/collections
```

## Convenciones de Código

- **Nombrado**: PascalCase para clases, camelCase para variables
- **Async/Await**: Todos los métodos I/O deben ser asíncronos
- **Inyección de Dependencias**: Usar constructor injection
- **Logging**: Usar ILogger<T> en todos los servicios
- **Excepciones**: Crear excepciones de dominio personalizadas

## Roadmap

### Fase 1: MVP
- [x] Definición de arquitectura
- [ ] Setup inicial de proyectos
- [ ] Configuración de bases de datos
- [ ] Implementación de carga de documentos
- [ ] Integración de OCR básico
- [ ] Búsqueda por texto simple

### Fase 2: IA y Vectorización
- [ ] Integración de Semantic Kernel
- [ ] Generación de embeddings
- [ ] Búsqueda semántica con Qdrant
- [ ] Chat básico con documentos (RAG)

### Fase 3: Mejoras
- [ ] Soporte multi-formato (PDF, DOCX, etc.)
- [ ] Procesamiento batch optimizado
- [ ] UI/UX mejorada en Blazor
- [ ] Métricas y analytics

### Fase 4: Producción
- [ ] Autenticación y autorización
- [ ] Multi-tenancy
- [ ] Backups automatizados
- [ ] Monitoring y logging avanzado

## Contribución

Este es un proyecto en desarrollo. Se aceptan sugerencias y mejoras.

## Licencia

[Definir licencia]
