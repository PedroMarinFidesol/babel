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

### Obtener Credenciales de Azure Computer Vision

Para usar Azure Computer Vision en modo cloud o para obtener credenciales para el contenedor local:

1. **Crear cuenta de Azure** (si no tienes una):
   - Visita [Azure Portal](https://portal.azure.com/)
   - Regístrate para obtener una cuenta gratuita (incluye $200 de crédito)

2. **Crear recurso de Computer Vision**:
   ```bash
   # Opción 1: Desde Azure Portal
   # - Busca "Computer Vision" en el marketplace
   # - Haz clic en "Create"
   # - Selecciona suscripción, grupo de recursos y región
   # - Elige el pricing tier (F0 es gratuito con límites)

   # Opción 2: Usando Azure CLI
   az cognitiveservices account create \
     --name babel-computer-vision \
     --resource-group babel-rg \
     --kind ComputerVision \
     --sku F0 \
     --location eastus
   ```

   [Documentacion de instalación](https://learn.microsoft.com/es-es/azure/ai-services/computer-vision/computer-vision-how-to-install-containers#get-the-container-image)

3. **Obtener credenciales**:
   - En Azure Portal, ve a tu recurso Computer Vision
   - En el menú lateral, selecciona "Keys and Endpoint"
   - Copia **Key 1** (o Key 2) y el **Endpoint**
   - Estas credenciales se usarán en el `docker-compose.yml` o `appsettings.json`

4. **Configuración para contenedor local**:
   - El contenedor de Azure OCR requiere estas credenciales para telemetría
   - Aunque se ejecuta localmente, necesita conectarse periódicamente a Azure
   - El billing se basa en el uso real del contenedor

## Configuración del Entorno de Desarrollo

### 1. Clonar el Repositorio

```bash
git clone https://github.com/tu-usuario/babel.git
cd babel
```

### 2. Descargar e Instalar Bases de Datos con Docker

#### SQL Server

**Opción A: Usar Docker Compose (Recomendado)**

Crear archivo `docker-compose.yml` en la raíz del proyecto:

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
    restart: unless-stopped

  qdrant:
    image: qdrant/qdrant:latest
    container_name: babel-qdrant
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant-data:/qdrant/storage
    restart: unless-stopped

  azure-ocr:
    image: mcr.microsoft.com/azure-cognitive-services/vision/read:3.2
    container_name: babel-azure-ocr
    environment:
      - EULA=accept
      - Billing=<YOUR_AZURE_ENDPOINT>
      - ApiKey=<YOUR_AZURE_API_KEY>
    ports:
      - "5000:5000"
    restart: unless-stopped

volumes:
  sqlserver-data:
  qdrant-data:
```

Iniciar todos los servicios:

```bash
docker-compose up -d
```

**Opción B: Instalación individual con Docker CLI**

```bash
# SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name babel-sqlserver \
  -v sqlserver-data:/var/opt/mssql \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Qdrant
docker run -p 6333:6333 -p 6334:6334 \
  --name babel-qdrant \
  -v qdrant-data:/qdrant/storage \
  -d qdrant/qdrant:latest

# Azure Computer Vision (OCR)
docker run --rm -it -p 5000:5000 \
  --name babel-azure-ocr \
  -e EULA=accept \
  -e Billing=<YOUR_AZURE_ENDPOINT> \
  -e ApiKey=<YOUR_AZURE_API_KEY> \
  mcr.microsoft.com/azure-cognitive-services/vision/read:3.2
```

#### Verificar que los servicios estén funcionando

```bash
# Verificar contenedores en ejecución
docker ps

# Verificar SQL Server
docker exec -it babel-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT @@VERSION"

# Verificar Qdrant
curl http://localhost:6333/collections

# Verificar Azure OCR (debe devolver un error 400 - es normal sin enviar imagen)
curl http://localhost:5000/vision/v3.2/read/analyze
```

### 3. Configuración de Qdrant

Qdrant se ejecuta automáticamente con Docker, pero necesitas crear la colección para almacenar los vectores:

#### Crear colección manualmente (Opcional - puede auto-crearse en startup)

```bash
# Usando curl
curl -X PUT 'http://localhost:6333/collections/documents' \
  -H 'Content-Type: application/json' \
  -d '{
    "vectors": {
      "size": 1536,
      "distance": "Cosine"
    }
  }'
```

**Notas importantes sobre Qdrant:**
- **Vector size (1536)**: Corresponde a text-embedding-ada-002 de OpenAI. Si usas otro modelo, ajusta este valor.
- **Distance metric**: "Cosine" es óptimo para embeddings de texto
- **Dashboard**: Accede a http://localhost:6333/dashboard para ver las colecciones y datos
- **Persistencia**: Los datos se almacenan en el volumen Docker `qdrant-data`

#### Dimensiones de vectores según modelo:

| Modelo de Embedding | Dimensiones |
|---------------------|-------------|
| text-embedding-ada-002 (OpenAI) | 1536 |
| text-embedding-3-small (OpenAI) | 1536 |
| text-embedding-3-large (OpenAI) | 3072 |
| nomic-embed-text (Ollama) | 768 |
| all-MiniLM-L6-v2 | 384 |

### 4. Configuración de Azure Computer Vision (OCR)

#### Opción A: Usar contenedor local (Recomendado para desarrollo)

El contenedor ya está incluido en el `docker-compose.yml`. Solo necesitas:

1. **Reemplazar las variables de entorno**:
   - `<YOUR_AZURE_ENDPOINT>`: El endpoint de tu recurso Azure Computer Vision
   - `<YOUR_AZURE_API_KEY>`: Una de las claves de tu recurso

2. **Ejemplo**:
   ```yaml
   azure-ocr:
     image: mcr.microsoft.com/azure-cognitive-services/vision/read:3.2
     container_name: babel-azure-ocr
     environment:
       - EULA=accept
       - Billing=https://babel-cv.cognitiveservices.azure.com/
       - ApiKey=abc123def456ghi789jkl012mno345pq
     ports:
       - "5000:5000"
   ```

3. **Uso local**:
   - El contenedor procesa las imágenes localmente (más rápido, sin latencia de red)
   - Solo se conecta a Azure para telemetría y validación de licencia
   - El billing se basa en transacciones, no en tiempo de ejecución

#### Opción B: Usar Azure Computer Vision Cloud

Si prefieres usar el servicio cloud directamente (sin contenedor):

1. **Configurar en appsettings.json**:
   ```json
   "AzureComputerVision": {
     "Endpoint": "https://babel-cv.cognitiveservices.azure.com/",
     "ApiKey": "tu-api-key-aqui",
     "UseLocalContainer": false
   }
   ```

2. **Ventajas**:
   - No requiere contenedor Docker
   - Siempre actualizado con últimas mejoras
   - Escalabilidad automática

3. **Desventajas**:
   - Latencia de red
   - Requiere conexión a internet
   - Costos por transacción

#### Límites y Precios de Azure Computer Vision

**Tier Gratuito (F0)**:
- 5,000 transacciones/mes gratis
- Hasta 20 llamadas/minuto
- Ideal para desarrollo y pruebas

**Tier Estándar (S1)**:
- $1.00 por 1,000 transacciones (0-1M)
- Precios decrecientes a mayor volumen
- Sin límite de llamadas/minuto

### 5. Configurar appsettings.json

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

### 6. Aplicar Migraciones de Base de Datos

```bash
cd Babel.Infrastructure
dotnet ef database update --startup-project ../Babel.API
```

### 7. Instalar Ollama (Opcional)

Si usarás LLMs locales:

```bash
# Instalar Ollama desde https://ollama.ai/
ollama pull llama2
ollama pull nomic-embed-text  # Para embeddings
```

### 8. Ejecutar la Aplicación

```bash
cd Babel.API
dotnet run
```

La aplicación estará disponible en:
- **Blazor UI**: https://localhost:5001
- **Hangfire Dashboard**: https://localhost:5001/hangfire
- **Qdrant Dashboard**: http://localhost:6333/dashboard
- **SQL Server**: localhost,1433 (usuario: sa, password: YourStrong@Passw0rd)
- **Azure OCR**: http://localhost:5000

## Solución de Problemas de Instalación

### SQL Server no inicia

**Error: "SQL Server container exits immediately"**

```bash
# Verificar logs
docker logs babel-sqlserver

# Causa común: Contraseña débil
# Solución: Usar contraseña con mayúsculas, minúsculas, números y símbolos
# Ejemplo: YourStrong@Passw0rd

# Reiniciar contenedor
docker rm babel-sqlserver
docker-compose up -d sqlserver
```

**Error: "Cannot connect to SQL Server"**

```bash
# Verificar que el contenedor esté corriendo
docker ps | grep babel-sqlserver

# Verificar puerto
netstat -an | grep 1433

# Si el puerto está ocupado, cambiar en docker-compose.yml:
# ports:
#   - "1434:1433"  # Usar 1434 en lugar de 1433
```

### Qdrant no inicia o no guarda datos

**Error: "Connection refused to Qdrant"**

```bash
# Verificar contenedor
docker logs babel-qdrant

# Reiniciar Qdrant
docker restart babel-qdrant

# Verificar con curl
curl http://localhost:6333/collections
```

**Problema: Datos se pierden al reiniciar**

```bash
# Verificar que el volumen esté montado
docker inspect babel-qdrant | grep -A 5 Mounts

# Si no hay volumen, recrear contenedor con volumen
docker-compose down
docker-compose up -d
```

### Azure Computer Vision (OCR) - Problemas comunes

**Error: "Container exits with EULA error"**

```bash
# Asegurar que EULA=accept esté en las variables de entorno
# Verificar docker-compose.yml:
# environment:
#   - EULA=accept
```

**Error: "Billing endpoint not valid"**

```bash
# Verificar formato del endpoint (debe terminar con /)
# Correcto: https://babel-cv.cognitiveservices.azure.com/
# Incorrecto: https://babel-cv.cognitiveservices.azure.com

# Verificar que la ApiKey sea correcta (copiar desde Azure Portal)
```

**Error: "Container requires internet connection"**

El contenedor de Azure OCR requiere conexión a internet para:
- Validar la licencia
- Enviar telemetría
- Verificar el billing endpoint

**Solución: Sin credenciales de Azure**

Si no tienes credenciales de Azure, puedes usar Tesseract OCR como alternativa:

```bash
# Agregar al docker-compose.yml
tesseract-ocr:
  image: tesseractshadow/tesseract4re
  container_name: babel-tesseract
  ports:
    - "5001:8884"
  restart: unless-stopped
```

### Docker Desktop no está ejecutándose

**Windows/Mac:**

```bash
# Verificar que Docker Desktop esté abierto
# Buscar icono de Docker en la barra de tareas/menú

# Si no inicia, reinstalar Docker Desktop
```

**Error: "Cannot connect to Docker daemon"**

```bash
# Linux: Iniciar servicio Docker
sudo systemctl start docker

# Verificar estado
sudo systemctl status docker

# Agregar usuario al grupo docker (para no usar sudo)
sudo usermod -aG docker $USER
# Cerrar sesión y volver a entrar
```

### Puertos ocupados

**Error: "Port already in use"**

```bash
# Identificar proceso usando el puerto
# Windows:
netstat -ano | findstr :1433
taskkill /PID <PID> /F

# Linux/Mac:
lsof -i :1433
kill -9 <PID>

# O cambiar puerto en docker-compose.yml
```

### Problemas de rendimiento con Docker

**Contenedores lentos en Windows/Mac:**

1. **Aumentar recursos de Docker Desktop**:
   - Abrir Docker Desktop → Settings → Resources
   - Aumentar CPU a 4 cores
   - Aumentar Memory a 8GB
   - Apply & Restart

2. **Usar WSL 2 backend** (Windows):
   - Settings → General → Use WSL 2 based engine

### Verificación completa del entorno

Script para verificar que todo esté funcionando:

```bash
#!/bin/bash
echo "=== Verificando entorno Babel ==="

echo "1. Docker containers:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo -e "\n2. SQL Server:"
docker exec babel-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT 'SQL Server OK' as Status" 2>/dev/null && echo "✓ SQL Server funcionando" || echo "✗ SQL Server con problemas"

echo -e "\n3. Qdrant:"
curl -s http://localhost:6333/collections | grep -q "result" && echo "✓ Qdrant funcionando" || echo "✗ Qdrant con problemas"

echo -e "\n4. Azure OCR:"
curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/vision/v3.2/read/analyze | grep -q "400" && echo "✓ Azure OCR funcionando" || echo "✗ Azure OCR con problemas"

echo -e "\n5. Ollama (opcional):"
curl -s http://localhost:11434/api/tags >/dev/null && echo "✓ Ollama funcionando" || echo "○ Ollama no instalado (opcional)"

echo -e "\n=== Verificación completada ==="
```

**Guardar como `check-environment.sh` y ejecutar:**

```bash
chmod +x check-environment.sh
./check-environment.sh
```

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
