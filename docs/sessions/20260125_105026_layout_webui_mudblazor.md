# Sesion 2026-01-25 10:50 - Layout Inicial WebUI con MudBlazor

## Resumen de la Sesion

Se creo el proyecto **Babel.WebUI** con Blazor Server y MudBlazor 8.0.0. Se implemento el layout principal con AppBar, Drawer y tema personalizable, junto con componentes para tarjetas de proyectos, chat RAG, subida de archivos y un indicador de estado de servicios externos.

## Cambios Implementados

### 1. Proyecto Babel.WebUI

**Archivo:** `src/Babel.WebUI/Babel.WebUI.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Babel.Application\Babel.Application.csproj" />
    <ProjectReference Include="..\Babel.Infrastructure\Babel.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MudBlazor" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 2. DTOs en Babel.Application

**Archivo:** `src/Babel.Application/DTOs/ProjectDto.cs`
```csharp
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalDocuments { get; set; }
    public int ProcessedDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Archivo:** `src/Babel.Application/DTOs/DocumentDto.cs`
- DTO con propiedades de archivo y metodo `FileSizeFormatted`

**Archivo:** `src/Babel.Application/DTOs/ChatMessageDto.cs`
- `ChatMessageDto` con referencias a documentos
- `DocumentReferenceDto` para chips de documentos fuente
- `ChatRequestDto` y `ChatResponseDto` para API

### 3. Layout Principal

**Archivo:** `src/Babel.WebUI/Shared/MainLayout.razor`
- AppBar con logo, titulo "Babel", indicador de servicios y toggle dark/light mode
- Drawer mini con hover expand
- Integrado con `IHealthCheckService`

**Archivo:** `src/Babel.WebUI/Shared/NavMenu.razor`
- Navegacion: Inicio, Proyectos, Administracion (Usuarios, Configuracion)

### 4. Componentes

**Archivo:** `src/Babel.WebUI/Components/Health/ServiceStatusIndicator.razor`
- Menu desplegable con estado de SQL Server, Qdrant, Azure OCR
- Auto-refresh cada 30 segundos
- Iconos verde/rojo segun estado
- Tiempo de respuesta en milisegundos

**Archivo:** `src/Babel.WebUI/Components/Projects/ProjectCard.razor`
- Tarjeta de proyecto con nombre, descripcion, conteo de documentos
- Barra de progreso de procesamiento
- Variante "Nuevo Proyecto" con borde punteado
- Efecto hover con elevacion

**Archivo:** `src/Babel.WebUI/Components/Documents/FileUpload.razor`
- Zona de drag-and-drop
- Seleccion de archivos con InputFile
- Lista de archivos seleccionados con tamano
- Validacion de tamano maximo (50MB default)
- Barra de progreso durante upload

**Archivo:** `src/Babel.WebUI/Components/Chat/ChatWindow.razor`
- Historial de mensajes usuario/asistente
- Input con envio por Enter
- Indicador "Analizando documentos..." durante procesamiento
- Boton para limpiar conversacion

**Archivo:** `src/Babel.WebUI/Components/Chat/ChatMessage.razor`
- Mensaje con estilo diferenciado usuario (azul) / asistente (gris)
- Referencias a documentos como chips clicables
- Timestamp de mensaje

### 5. Servicio Mock

**Archivo:** `src/Babel.WebUI/Services/MockDataService.cs`
- 3 proyectos mock: Facturas 2024, Contratos Laborales, Manuales Tecnicos
- Documentos mock por proyecto
- Historial de chat por proyecto
- Respuestas simuladas con referencias a documentos

### 6. Pagina Principal

**Archivo:** `src/Babel.WebUI/Pages/Index.razor`
- Grid responsive de tarjetas de proyectos
- Tarjeta especial "Nuevo Proyecto"
- Seccion de demostracion con Chat y FileUpload
- Tabla de documentos del proyecto seleccionado
- Dialog para crear nuevo proyecto (mock)

### 7. Solucion Actualizada

**Archivo:** `Babel.slnx`
```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Babel.API/Babel.API.csproj" />
    <Project Path="src/Babel.Application/Babel.Application.csproj" />
    <Project Path="src/Babel.Domain/Babel.Domain.csproj" />
    <Project Path="src/Babel.Infrastructure/Babel.Infrastructure.csproj" />
    <Project Path="src/Babel.WebUI/Babel.WebUI.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Babel.Domain.Tests/Babel.Domain.Tests.csproj" />
  </Folder>
</Solution>
```

## Problemas Encontrados y Soluciones

### 1. Version de MudBlazor no existente

**Error:** `warning NU1603: Babel.WebUI depende de MudBlazor (>= 7.18.0), pero no se encontro`

**Solucion:** Actualizar a version 8.0.0 que es la actual disponible
```xml
<PackageReference Include="MudBlazor" Version="8.0.0" />
```

### 2. Atributo Class con contenido mixto en Razor

**Error:** `error RZ9986: Component attributes do not support complex content`
```razor
Class="@GetMessageClass() pa-3"  <!-- Incorrecto -->
```

**Solucion:** Usar interpolacion de string
```razor
Class="@($"{GetMessageClass()} pa-3")"  <!-- Correcto -->
```

### 3. Duplicacion de directivas de evento

**Error:** `error RZ10010: The component parameter 'ondrop' is used two or more times`

**Causa:** No se puede usar `@ondrop="handler"` y `@ondrop:preventDefault` simultaneamente

**Solucion:** Usar solo una de las dos opciones
```razor
@ondragover:preventDefault
@ondrop="HandleDrop"
```

### 4. Metodo AddInfrastructure no AddInfrastructureServices

**Error:** `error CS1061: "IServiceCollection" no contiene una definicion para "AddInfrastructureServices"`

**Solucion:** El metodo real se llama `AddInfrastructure`
```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

### 5. Atributo Title obsoleto en MudBlazor 8.0

**Warning:** `MUD0002: Illegal Attribute 'Title' on 'MudIconButton'`

**Solucion:** Usar MudTooltip envolviendo el componente
```razor
<MudTooltip Text="Modo claro">
    <MudIconButton Icon="@Icons.Material.Filled.LightMode" />
</MudTooltip>
```

## Desafios Tecnicos

1. **Blazor Server vs WebAssembly**: Se eligio Server para mejor integracion con servicios backend y health checks en tiempo real

2. **MudBlazor 8.0 breaking changes**: La version 8.0 introdujo cambios en atributos de componentes que requirieron ajustes

3. **Drag and Drop en Blazor**: Las directivas de eventos tienen limitaciones - no se pueden combinar handlers personalizados con :preventDefault

## Configuracion Final

**Archivo:** `src/Babel.WebUI/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Babel;..."
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333",
    "CollectionName": "babel_documents"
  },
  "AzureComputerVision": {
    "Endpoint": "http://localhost:5000"
  }
}
```

## Proximos Pasos

1. **Implementar pagina de detalle de proyecto** (`/project/{id}`)
2. **Conectar FileUpload con MediatR Commands** para subida real
3. **Implementar chat RAG real** con Semantic Kernel
4. **Crear migracion EF** para los nuevos campos de Document/DocumentChunk
5. **Agregar pagina de lista de proyectos** (`/projects`)

## Comandos Utiles

```bash
# Compilar solucion
dotnet build

# Ejecutar WebUI
cd src/Babel.WebUI
dotnet run

# Ejecutar tests
dotnet test

# Restaurar paquetes
dotnet restore
```

## Lecciones Aprendidas

1. **MudBlazor 8.0**: Verificar siempre la version actual disponible, no asumir versiones del plan
2. **Razor syntax**: Los atributos de componentes no soportan contenido mixto C#/texto
3. **Event directives**: Las directivas como `:preventDefault` generan handlers automaticamente
4. **Naming conventions**: Verificar nombres exactos de extension methods antes de usarlos

## Estado Final del Proyecto

- [x] Proyecto Babel.WebUI creado con MudBlazor 8.0
- [x] DTOs ProjectDto, DocumentDto, ChatMessageDto
- [x] Layout con AppBar, Drawer, tema claro/oscuro
- [x] ServiceStatusIndicator con health checks
- [x] ProjectCard con variante "Nuevo Proyecto"
- [x] FileUpload drag-and-drop
- [x] ChatWindow con mensajes y referencias
- [x] MockDataService con datos de prueba
- [x] Index.razor con grid de proyectos y demos
- [x] Build sin errores ni warnings
- [x] 27 tests pasando
