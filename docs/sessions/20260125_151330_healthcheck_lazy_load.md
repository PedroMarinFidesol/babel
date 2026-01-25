# Sesion 2026-01-25 15:13 - Health Checks Lazy Load y Correccion de Configuracion

## Resumen de la Sesion

Se optimizo el componente ServiceStatusIndicator para que los health checks solo se ejecuten bajo demanda (al pulsar el boton), eliminando la carga automatica al inicio que causaba retrasos en el arranque de la aplicacion. Tambien se corrigio un bug donde WebUI no cargaba `appsettings.local.json`, causando fallos en la conexion a SQL Server.

## Cambios Implementados

### 1. ServiceStatusIndicator.razor - Health checks bajo demanda

**Archivo:** `src/Babel.WebUI/Components/Health/ServiceStatusIndicator.razor`

**Antes:** El componente ejecutaba health checks automaticamente en `OnInitializedAsync` y cada 30 segundos con un timer.

**Despues:** Los health checks solo se ejecutan cuando el usuario pulsa el boton de refresh.

```razor
@inject IHealthCheckService HealthCheckService

<MudMenu @ref="_menu"
         Icon="@GetOverallStatusIcon()"
         Color="@GetOverallStatusColor()"
         Dense="true"
         AnchorOrigin="Origin.BottomRight"
         TransformOrigin="Origin.TopRight">
    <ChildContent>
        <MudList T="string" Dense="true" Class="pa-0" Style="min-width: 280px;">
            <MudListSubheader Class="py-1">
                <div class="d-flex align-center justify-space-between" style="width: 100%;">
                    <MudText Typo="Typo.overline">Estado de Servicios</MudText>
                    <MudIconButton Icon="@Icons.Material.Filled.Refresh"
                                   Size="Size.Small"
                                   OnClick="@RefreshStatusAsync"
                                   Disabled="@_isLoading" />
                </div>
            </MudListSubheader>
            <!-- ... contenido del menu ... -->
        </MudList>
    </ChildContent>
</MudMenu>

@code {
    private MudMenu? _menu;
    private List<HealthCheckResult> _serviceStatuses = [];
    private DateTime? _lastCheck;
    private bool _isLoading;

    private async Task RefreshStatusAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var sqlTask = HealthCheckService.CheckSqlServerAsync(cts.Token);
            var qdrantTask = HealthCheckService.CheckQdrantAsync(cts.Token);
            var ocrTask = HealthCheckService.CheckAzureOcrAsync(cts.Token);

            await Task.WhenAll(sqlTask, qdrantTask, ocrTask);

            _serviceStatuses = [await sqlTask, await qdrantTask, await ocrTask];
            _lastCheck = DateTime.Now;
        }
        // ... manejo de errores ...
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
    // ...
}
```

**Cambios clave:**
- Eliminado `OnInitializedAsync` con llamada automatica a `RefreshStatusAsync`
- Eliminado el `Timer` de auto-refresh cada 30 segundos
- Eliminado `IDisposable` (ya no hay timer que disponer)
- Posicion del menu corregida: `AnchorOrigin="Origin.BottomRight"` y `TransformOrigin="Origin.TopRight"`
- Estado inicial muestra icono de nube (`CloudQueue`) en lugar de "cargando"
- Mensaje "Pulsa actualizar para comprobar" cuando no hay datos

### 2. Program.cs WebUI - Carga de appsettings.local.json

**Archivo:** `src/Babel.WebUI/Program.cs`

**Problema:** WebUI no cargaba `appsettings.local.json`, usando la connection string incorrecta del `appsettings.json` base.

**Solucion:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Load local settings with secrets (not committed to git)
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add MudBlazor services
builder.Services.AddMudServices();
// ...
```

### 3. MainLayout.razor - Limpieza de inyeccion no usada

**Archivo:** `src/Babel.WebUI/Shared/MainLayout.razor`

Se elimino la inyeccion de `IHealthCheckService` que no se usaba directamente en el layout:

```razor
@inherits LayoutComponentBase
@* Eliminado: @inject IHealthCheckService HealthCheckService *@
```

### 4. DependencyInjection.cs - Timeout reducido

**Archivo:** `src/Babel.Infrastructure/DependencyInjection.cs`

Timeout del HttpClient de Azure OCR reducido de 30s a 10s:

```csharp
services.AddHttpClient("AzureOcr", client =>
{
    client.BaseAddress = new Uri(azureOcrEndpoint);
    client.Timeout = TimeSpan.FromSeconds(10);  // Antes: 30 segundos
});
```

## Problemas Encontrados y Soluciones

### Problema 1: Inicio lento de la aplicacion

**Sintoma:** La aplicacion WebUI tardaba mucho en mostrar la interfaz.

**Causa raiz:** El componente `ServiceStatusIndicator` ejecutaba los health checks en `OnInitializedAsync`, bloqueando el renderizado inicial hasta que los 3 servicios respondieran (o expiraran los timeouts).

**Solucion:** Cambiar a ejecucion bajo demanda. Los health checks solo se ejecutan cuando el usuario abre el menu y pulsa el boton de refresh.

### Problema 2: SQL Server health check fallaba en WebUI pero funcionaba en API

**Sintoma:** El health check de SQL Server mostraba error en WebUI, pero el endpoint `/health` de la API confirmaba que estaba funcionando.

**Causa raiz:** El API cargaba `appsettings.local.json` explicitamente, pero WebUI no lo hacia. Las connection strings eran diferentes:

| Archivo | Password |
|---------|----------|
| `appsettings.json` | `YourStrong@Passw0rd` |
| `appsettings.local.json` | `Creame123!` |

**Solucion:** Agregar la linea de carga de `appsettings.local.json` en `Program.cs` de WebUI.

### Problema 3: Menu de status se salia por la izquierda

**Sintoma:** El listado de estado de servicios se salia por el lado izquierdo de la pantalla.

**Causa raiz:** Los valores de `AnchorOrigin` y `TransformOrigin` del `MudMenu` estaban centrados.

**Solucion:** Cambiar a:
```razor
AnchorOrigin="Origin.BottomRight"
TransformOrigin="Origin.TopRight"
```

## Configuracion Final

**appsettings.local.json** (identico en API y WebUI):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BabelDb;User Id=sa;Password=Creame123!;TrustServerCertificate=True;Encrypt=True;"
  },
  "AzureComputerVision": {
    "ApiKey": "***"
  }
}
```

## Proximos Pasos

1. Implementar Commands/Queries con MediatR
2. Crear migracion EF para los nuevos campos de entidades
3. Conectar WebUI con servicios reales (eliminar MockDataService)
4. Implementar FileStorageService para almacenamiento de archivos
5. Servicio de chunking para dividir documentos

## Comandos Utiles

```bash
# Compilar WebUI
dotnet build src/Babel.WebUI/Babel.WebUI.csproj

# Ejecutar WebUI
dotnet run --project src/Babel.WebUI/Babel.WebUI.csproj
```

## Lecciones Aprendidas

1. **No bloquear renderizado inicial:** En Blazor Server, evitar operaciones asincronas largas en `OnInitializedAsync`. Usar `OnAfterRenderAsync` o ejecucion bajo demanda para operaciones que pueden tardar.

2. **Configuracion consistente:** Asegurar que todos los proyectos de la solucion carguen los mismos archivos de configuracion (`appsettings.local.json`).

3. **Timeouts cortos para health checks:** Los health checks deben tener timeouts cortos (5-10 segundos) para no bloquear la UX.

4. **Posicionamiento de menus MudBlazor:** Usar `AnchorOrigin` y `TransformOrigin` apropiados segun la posicion del elemento en la pantalla.

## Estado Final del Proyecto

- [x] Health checks ejecutados solo bajo demanda
- [x] Menu de status posicionado correctamente
- [x] WebUI carga appsettings.local.json
- [x] Timeout de Azure OCR HttpClient reducido
- [x] Eliminada inyeccion no usada en MainLayout
