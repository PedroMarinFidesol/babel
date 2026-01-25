# Babel - Guía de Inicio Rápido

## Requisitos Previos

- .NET 10.0 SDK
- Docker Desktop
- (Opcional) Ollama para LLM local

## 1. Iniciar Servicios Docker

```bash
# Iniciar SQL Server y Qdrant
docker-compose up -d sqlserver qdrant

# Verificar que los servicios estén corriendo
docker-compose ps

# Ver logs si hay problemas
docker-compose logs -f sqlserver
docker-compose logs -f qdrant
```

**Nota:** El servicio Azure OCR requiere credenciales de Azure. Para desarrollo sin OCR:
```bash
# Iniciar solo SQL Server y Qdrant (sin OCR)
docker-compose up -d sqlserver qdrant
```

Para usar OCR (requiere configurar .env con credenciales de Azure):
```bash
docker-compose --profile ocr up -d
```

## 2. Configurar Variables de Entorno

```bash
# Copiar archivo de ejemplo
cp .env.example .env

# Editar .env con tus credenciales (opcional para desarrollo básico)
```

## 3. Aplicar Migraciones de Base de Datos

```bash
# Desde la raíz del proyecto
dotnet ef database update --project src/Babel.Infrastructure --startup-project src/Babel.API
```

## 4. Ejecutar la Aplicación

### API (Backend)
```bash
cd src/Babel.API
dotnet run
```

La API estará disponible en:
- API: https://localhost:5001
- Swagger: https://localhost:5001/swagger
- Health Check: https://localhost:5001/health

### WebUI (Frontend)
```bash
cd src/Babel.WebUI
dotnet run
```

## 5. Verificar Servicios

### SQL Server
```bash
# Conectar al contenedor
docker exec -it babel-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "SELECT name FROM sys.databases"
```

### Qdrant
```bash
# Verificar que Qdrant esté respondiendo
curl http://localhost:6333/collections

# Dashboard de Qdrant
# Abrir en navegador: http://localhost:6333/dashboard
```

## 6. Detener Servicios

```bash
# Detener todos los servicios
docker-compose down

# Detener y eliminar volúmenes (CUIDADO: elimina datos)
docker-compose down -v
```

## Puertos Utilizados

| Servicio | Puerto | Descripción |
|----------|--------|-------------|
| SQL Server | 1433 | Base de datos principal |
| Qdrant REST | 6333 | API REST de Qdrant |
| Qdrant gRPC | 6334 | API gRPC de Qdrant |
| Azure OCR | 5000 | Servicio de OCR local |
| Babel API | 5001 | API REST de Babel |
| Babel WebUI | 5002 | Interfaz web de Babel |

## Credenciales por Defecto (Desarrollo)

| Servicio | Usuario | Password |
|----------|---------|----------|
| SQL Server | sa | YourStrong@Passw0rd |

## Comandos Útiles

### Migraciones EF Core
```bash
# Crear nueva migración
dotnet ef migrations add NombreMigracion --project src/Babel.Infrastructure --startup-project src/Babel.API

# Aplicar migraciones
dotnet ef database update --project src/Babel.Infrastructure --startup-project src/Babel.API

# Revertir última migración
dotnet ef migrations remove --project src/Babel.Infrastructure --startup-project src/Babel.API
```

### Tests
```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Build
```bash
# Compilar solución
dotnet build

# Compilar en modo Release
dotnet build -c Release
```

## Solución de Problemas

### Error de conexión a SQL Server
1. Verificar que el contenedor esté corriendo: `docker ps`
2. Verificar logs: `docker logs babel-sqlserver`
3. Asegurar que el puerto 1433 no esté en uso

### Error de conexión a Qdrant
1. Verificar que el contenedor esté corriendo: `docker ps`
2. Probar endpoint: `curl http://localhost:6333/collections`
3. Verificar logs: `docker logs babel-qdrant`

### La aplicación no arranca
1. Verificar que los servicios Docker estén corriendo
2. Revisar los logs de la aplicación
3. Verificar la cadena de conexión en appsettings.Development.json
