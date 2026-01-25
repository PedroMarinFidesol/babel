# Sesión 2025-01-25 09:39 - Configuración de Secrets con appsettings.local.json

## Resumen de la sesión

Se implementó un sistema para proteger las claves sensibles del repositorio usando un archivo `appsettings.local.json` que no se sube a Git.

## Cambios implementados

### 1. Actualización de `.gitignore`

Se añadieron las siguientes líneas para ignorar archivos de configuración local:

```gitignore
# Local settings with secrets (do not commit)
appsettings.local.json
appsettings.*.local.json
```

### 2. Creación de `appsettings.local.json.example`

Plantilla para que otros desarrolladores sepan qué configurar:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BabelDb;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;Encrypt=True;"
  },
  "AzureComputerVision": {
    "ApiKey": "YOUR_AZURE_COMPUTER_VISION_API_KEY_HERE"
  }
}
```

### 3. Limpieza de `appsettings.json`

Se eliminaron las claves sensibles, dejando valores vacíos:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "AzureComputerVision": {
    "ApiKey": ""
  }
}
```

### 4. Simplificación de `appsettings.Development.json`

Solo contiene configuración de logging, sin secretos:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### 5. Actualización de `Program.cs`

Se añadió la carga del archivo local de configuración:

```csharp
// Load local settings with secrets (not committed to git)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
```

## Archivos modificados

| Archivo | Acción |
|---------|--------|
| `.gitignore` | Modificado - Añadidas reglas para ignorar appsettings.local.json |
| `src/Babel.API/appsettings.json` | Modificado - Claves sensibles vaciadas |
| `src/Babel.API/appsettings.Development.json` | Modificado - Solo logging |
| `src/Babel.API/appsettings.local.json` | Creado - Contiene secretos reales (ignorado) |
| `src/Babel.API/appsettings.local.json.example` | Creado - Plantilla para desarrolladores |
| `src/Babel.API/Program.cs` | Modificado - Carga appsettings.local.json |

## Cómo funciona el sistema de configuración

El orden de carga de configuración en .NET es:

1. `appsettings.json` → Configuración base (sin secretos)
2. `appsettings.{Environment}.json` → Configuración por entorno
3. `appsettings.local.json` → **Secretos locales** (no se sube al repo)

Los valores posteriores sobrescriben los anteriores, permitiendo que los secretos en `appsettings.local.json` reemplacen los valores vacíos.

## Para nuevos desarrolladores

Cuando alguien clone el repositorio debe:

1. Copiar `appsettings.local.json.example` a `appsettings.local.json`
2. Rellenar los valores con sus propias credenciales
3. El archivo `appsettings.local.json` nunca se subirá al repositorio

## Comandos ejecutados

```bash
# Commit de los cambios
git add .gitignore src/Babel.API/Program.cs src/Babel.API/appsettings.Development.json src/Babel.API/appsettings.json src/Babel.API/appsettings.local.json.example
git commit -m "feat: move secrets to appsettings.local.json"
```

## Lecciones aprendidas

- La opción `optional: true` en `AddJsonFile` permite que la aplicación arranque incluso si el archivo no existe
- Es importante crear un archivo `.example` para documentar qué configuración se espera
- Los archivos de configuración se cargan en orden y los valores posteriores sobrescriben los anteriores

## Estado final del proyecto

- [x] Secretos protegidos del repositorio
- [x] Plantilla para otros desarrolladores
- [x] Configuración cargada correctamente en Program.cs
- [x] Commit realizado

## Próximos pasos

- Continuar con la implementación de la Fase 2: Gestión de Proyectos y Documentos
- Implementar Commands/Queries con MediatR
- Crear Controllers para Projects y Documents
