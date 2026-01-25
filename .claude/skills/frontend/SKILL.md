---
name: frontend
description: Agente especializado en desarrollo frontend con Blazor Server y MudBlazor para el proyecto Babel
---

# Frontend Specialist Skill (MudBlazor)

Skill especializado para desarrollo de UI en Babel usando Blazor Server y MudBlazor.

## Uso

```
/frontend                              # Guía general
/frontend create project cards         # Implementar tarjetas de proyectos
/frontend implement file upload        # Componente de carga de archivos
/frontend setup                        # Configuración inicial
```

---

## 1. Configuración Inicial de Babel.WebUI

### Paso 1: Crear el Proyecto

```bash
# Desde la raíz del repositorio
dotnet new blazorserver -n Babel.WebUI -o Babel.WebUI
cd Babel.WebUI

# Agregar a la solución
cd ..
dotnet sln add Babel.WebUI/Babel.WebUI.csproj

# Agregar referencias a otros proyectos
dotnet add Babel.WebUI/Babel.WebUI.csproj reference Babel.Application/Babel.Application.csproj
dotnet add Babel.WebUI/Babel.WebUI.csproj reference Babel.Infrastructure/Babel.Infrastructure.csproj
```

### Paso 2: Instalar MudBlazor

```bash
cd Babel.WebUI
dotnet add package MudBlazor
```

### Paso 3: Configurar MudBlazor en Program.cs

```csharp
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
});

// Add Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

### Paso 4: Configurar _Imports.razor

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using MudBlazor
@using Babel.WebUI
@using Babel.WebUI.Shared
@using Babel.WebUI.Components
@using Babel.Application.DTOs
@using MediatR
```

### Paso 5: Configurar _Host.cshtml

```html
@page "/"
@namespace Babel.WebUI.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Babel - Gestión Documental</title>
    <base href="~/" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
</head>
<body>
    <component type="typeof(App)" render-mode="ServerPrerendered" />

    <script src="_framework/blazor.server.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

### Paso 6: Configurar App.razor

```razor
<MudThemeProvider @bind-IsDarkMode="@_isDarkMode" Theme="_theme" />
<MudDialogProvider />
<MudSnackbarProvider />

<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <MudText Typo="Typo.h6">Página no encontrada</MudText>
        </LayoutView>
    </NotFound>
</Router>

@code {
    private bool _isDarkMode = false;
    private MudTheme _theme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Default
        },
        PaletteDark = new PaletteDark()
        {
            Primary = Colors.Blue.Lighten1
        }
    };
}
```

---

## 2. Estructura de Carpetas Recomendada

```
Babel.WebUI/
├── wwwroot/
│   ├── css/
│   │   └── app.css
│   └── images/
├── Pages/
│   ├── Index.razor              # Vista principal con tarjetas de proyectos
│   ├── Project/
│   │   └── Detail.razor         # Detalle de proyecto (chat + archivos + upload)
│   └── _Host.cshtml
├── Shared/
│   ├── MainLayout.razor         # Layout principal con AppBar y Drawer
│   └── NavMenu.razor            # Menú de navegación
├── Components/
│   ├── Projects/
│   │   ├── ProjectCard.razor    # Tarjeta individual de proyecto
│   │   └── ProjectForm.razor    # Formulario crear/editar proyecto
│   ├── Documents/
│   │   ├── FileList.razor       # MudDataGrid de documentos
│   │   ├── FileUpload.razor     # Componente drag-and-drop
│   │   └── OcrReview.razor      # Revisión de OCR
│   └── Chat/
│       ├── ChatWindow.razor     # Ventana de chat RAG
│       └── ChatMessage.razor    # Mensaje individual
├── Services/
│   └── IProjectStateService.cs  # Estado compartido entre componentes
├── _Imports.razor
├── App.razor
└── Program.cs
```

---

## 3. Componentes Principales

### 3.1 MainLayout.razor (Layout Principal)

```razor
@inherits LayoutComponentBase
@inject NavigationManager NavigationManager

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu"
                       Color="Color.Inherit"
                       Edge="Edge.Start"
                       OnClick="@ToggleDrawer" />
        <MudText Typo="Typo.h5" Class="ml-3">Babel</MudText>
        <MudSpacer />
        <MudIconButton Icon="@Icons.Material.Filled.Brightness4"
                       Color="Color.Inherit"
                       OnClick="@ToggleDarkMode" />
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" ClipMode="DrawerClipMode.Always" Elevation="2">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="py-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private bool _isDarkMode = false;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private void ToggleDarkMode() => _isDarkMode = !_isDarkMode;
}
```

### 3.2 Index.razor (Tarjetas de Proyectos)

```razor
@page "/"
@inject IMediator Mediator
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar

<PageTitle>Proyectos - Babel</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Mis Proyectos</MudText>

<MudGrid>
    @if (_isLoading)
    {
        @for (int i = 0; i < 6; i++)
        {
            <MudItem xs="12" sm="6" md="4">
                <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="200px" />
            </MudItem>
        }
    }
    else
    {
        <MudItem xs="12" sm="6" md="4">
            <MudCard Elevation="2" Class="cursor-pointer hover-card"
                     Style="min-height: 200px; border: 2px dashed var(--mud-palette-primary);"
                     @onclick="OpenCreateDialog">
                <MudCardContent Class="d-flex flex-column align-center justify-center" Style="height: 200px;">
                    <MudIcon Icon="@Icons.Material.Filled.Add" Size="Size.Large" Color="Color.Primary" />
                    <MudText Typo="Typo.h6" Color="Color.Primary" Class="mt-2">Nuevo Proyecto</MudText>
                </MudCardContent>
            </MudCard>
        </MudItem>

        @foreach (var project in _projects)
        {
            <MudItem xs="12" sm="6" md="4">
                <ProjectCard Project="project"
                             OnClick="@(() => NavigateToProject(project.Id))"
                             OnDelete="@(() => DeleteProject(project.Id))" />
            </MudItem>
        }
    }
</MudGrid>

@code {
    private List<ProjectDto> _projects = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadProjects();
    }

    private async Task LoadProjects()
    {
        _isLoading = true;
        try
        {
            var result = await Mediator.Send(new GetProjectsQuery());
            _projects = result.ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error al cargar proyectos: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void NavigateToProject(Guid id)
    {
        NavigationManager.NavigateTo($"/project/{id}");
    }

    private async Task OpenCreateDialog()
    {
        // Implementar diálogo de creación
    }

    private async Task DeleteProject(Guid id)
    {
        // Implementar confirmación y eliminación
    }
}
```

### 3.3 ProjectCard.razor (Tarjeta de Proyecto)

```razor
<MudCard Elevation="2" Class="cursor-pointer hover-card" Style="min-height: 200px;">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">@Project.Name</MudText>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudMenu Icon="@Icons.Material.Filled.MoreVert">
                <MudMenuItem Icon="@Icons.Material.Filled.Edit" OnClick="@(() => OnEdit.InvokeAsync(Project.Id))">
                    Editar
                </MudMenuItem>
                <MudMenuItem Icon="@Icons.Material.Filled.Delete" OnClick="@(() => OnDelete.InvokeAsync(Project.Id))">
                    Eliminar
                </MudMenuItem>
            </MudMenu>
        </CardHeaderActions>
    </MudCardHeader>

    <MudCardContent @onclick="@(() => OnClick.InvokeAsync())">
        <MudStack Row="true" Spacing="4">
            <MudStack AlignItems="AlignItems.Center">
                <MudIcon Icon="@Icons.Material.Filled.Description" Size="Size.Large" Color="Color.Primary" />
                <MudText Typo="Typo.h4">@Project.DocumentCount</MudText>
                <MudText Typo="Typo.caption" Color="Color.Secondary">Documentos</MudText>
            </MudStack>
            <MudDivider Vertical="true" FlexItem="true" />
            <MudStack AlignItems="AlignItems.Center">
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Size="Size.Large" Color="Color.Success" />
                <MudText Typo="Typo.h4">@Project.ProcessedCount</MudText>
                <MudText Typo="Typo.caption" Color="Color.Secondary">Procesados</MudText>
            </MudStack>
        </MudStack>
    </MudCardContent>

    <MudCardActions>
        <MudChip T="string" Size="Size.Small" Color="Color.Info">
            @Project.CreatedAt.ToString("dd/MM/yyyy")
        </MudChip>
    </MudCardActions>
</MudCard>

@code {
    [Parameter] public ProjectDto Project { get; set; } = default!;
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public EventCallback<Guid> OnEdit { get; set; }
    [Parameter] public EventCallback<Guid> OnDelete { get; set; }
}

<style>
    .hover-card:hover {
        transform: translateY(-4px);
        transition: transform 0.2s ease-in-out;
        box-shadow: 0 8px 16px rgba(0,0,0,0.2);
    }
</style>
```

### 3.4 FileList.razor (MudDataGrid de Documentos)

```razor
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudDataGrid T="DocumentDto"
             Items="@Documents"
             SortMode="SortMode.Multiple"
             Filterable="true"
             FilterMode="DataGridFilterMode.Simple"
             Hideable="true"
             Striped="true"
             Dense="true"
             Hover="true"
             Loading="@_isLoading"
             RowClick="@OnRowClick">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Documentos del Proyecto</MudText>
        <MudSpacer />
        <MudTextField @bind-Value="_searchString"
                      Placeholder="Buscar..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      IconSize="Size.Medium"
                      Class="mt-0"
                      Immediate="true"
                      DebounceInterval="300" />
    </ToolBarContent>

    <Columns>
        <PropertyColumn Property="x => x.FileName" Title="Nombre" Sortable="true" Filterable="true">
            <CellTemplate>
                <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                    <MudIcon Icon="@GetFileIcon(context.Item.FileExtension)" Size="Size.Small" />
                    <MudText>@context.Item.FileName</MudText>
                </MudStack>
            </CellTemplate>
        </PropertyColumn>

        <PropertyColumn Property="x => x.FileExtension" Title="Tipo" Sortable="true" />

        <PropertyColumn Property="x => x.Status" Title="Estado" Sortable="true">
            <CellTemplate>
                <MudChip T="string" Size="Size.Small" Color="@GetStatusColor(context.Item.Status)">
                    @context.Item.Status.ToString()
                </MudChip>
            </CellTemplate>
        </PropertyColumn>

        <PropertyColumn Property="x => x.CreatedAt" Title="Fecha" Sortable="true" Format="dd/MM/yyyy HH:mm" />

        <TemplateColumn Title="Acciones" Sortable="false">
            <CellTemplate>
                <MudStack Row="true" Spacing="1">
                    <MudIconButton Icon="@Icons.Material.Filled.Visibility"
                                   Size="Size.Small"
                                   OnClick="@(() => ViewDocument(context.Item))" />
                    @if (context.Item.RequiresOcr && !context.Item.OcrReviewed)
                    {
                        <MudIconButton Icon="@Icons.Material.Filled.RateReview"
                                       Size="Size.Small"
                                       Color="Color.Warning"
                                       OnClick="@(() => ReviewOcr(context.Item))" />
                    }
                    <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                   Size="Size.Small"
                                   Color="Color.Error"
                                   OnClick="@(() => DeleteDocument(context.Item))" />
                </MudStack>
            </CellTemplate>
        </TemplateColumn>
    </Columns>

    <PagerContent>
        <MudDataGridPager T="DocumentDto" />
    </PagerContent>
</MudDataGrid>

@code {
    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public List<DocumentDto> Documents { get; set; } = new();
    [Parameter] public EventCallback<DocumentDto> OnDocumentSelected { get; set; }

    private bool _isLoading = false;
    private string _searchString = "";

    private async Task OnRowClick(DataGridRowClickEventArgs<DocumentDto> args)
    {
        await OnDocumentSelected.InvokeAsync(args.Item);
    }

    private string GetFileIcon(string extension) => extension?.ToLower() switch
    {
        ".pdf" => Icons.Custom.FileFormats.FilePdf,
        ".doc" or ".docx" => Icons.Custom.FileFormats.FileWord,
        ".xls" or ".xlsx" => Icons.Custom.FileFormats.FileExcel,
        ".jpg" or ".jpeg" or ".png" or ".gif" => Icons.Custom.FileFormats.FileImage,
        ".txt" or ".md" => Icons.Custom.FileFormats.FileDocument,
        _ => Icons.Material.Filled.InsertDriveFile
    };

    private Color GetStatusColor(DocumentStatus status) => status switch
    {
        DocumentStatus.Pending => Color.Default,
        DocumentStatus.Processing => Color.Info,
        DocumentStatus.Completed => Color.Success,
        DocumentStatus.Failed => Color.Error,
        _ => Color.Default
    };

    private void ViewDocument(DocumentDto document) { /* Implementar */ }
    private void ReviewOcr(DocumentDto document) { /* Implementar */ }
    private void DeleteDocument(DocumentDto document) { /* Implementar */ }
}
```

### 3.5 FileUpload.razor (Drag and Drop)

```razor
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudPaper @ondragenter="@HandleDragEnter"
          @ondragleave="@HandleDragLeave"
          @ondragover:preventDefault
          @ondrop="@HandleDrop"
          Class="@GetDropZoneClass()"
          Elevation="0"
          Style="min-height: 200px; border: 2px dashed var(--mud-palette-lines-default); transition: all 0.3s;">
    <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Class="pa-8">
        @if (_isUploading)
        {
            <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
            <MudText Typo="Typo.body1">Subiendo @_uploadProgress.Count archivos...</MudText>
            <MudProgressLinear Color="Color.Primary"
                               Value="@_uploadProgressPercent"
                               Class="my-4"
                               Style="width: 80%;" />
        }
        else
        {
            <MudIcon Icon="@Icons.Material.Filled.CloudUpload"
                     Size="Size.Large"
                     Color="@(_isDragOver ? Color.Primary : Color.Default)" />
            <MudText Typo="Typo.h6" Color="@(_isDragOver ? Color.Primary : Color.Default)">
                Arrastra archivos aquí
            </MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">
                o haz clic para seleccionar
            </MudText>
            <InputFile id="fileInput"
                       OnChange="HandleFileSelected"
                       multiple
                       hidden
                       accept=".pdf,.doc,.docx,.txt,.md,.jpg,.jpeg,.png,.tiff,.bmp" />
            <MudButton HtmlTag="label"
                       Variant="Variant.Filled"
                       Color="Color.Primary"
                       StartIcon="@Icons.Material.Filled.AttachFile"
                       for="fileInput"
                       Class="mt-4">
                Seleccionar Archivos
            </MudButton>
        }
    </MudStack>
</MudPaper>

@if (_selectedFiles.Any())
{
    <MudTable Items="@_selectedFiles" Class="mt-4" Dense="true">
        <HeaderContent>
            <MudTh>Archivo</MudTh>
            <MudTh>Tamaño</MudTh>
            <MudTh>Estado</MudTh>
            <MudTh></MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd>@context.Name</MudTd>
            <MudTd>@FormatFileSize(context.Size)</MudTd>
            <MudTd>
                @if (_uploadProgress.TryGetValue(context.Name, out var progress))
                {
                    <MudChip T="string" Size="Size.Small" Color="@GetProgressColor(progress)">
                        @progress
                    </MudChip>
                }
                else
                {
                    <MudChip T="string" Size="Size.Small">Pendiente</MudChip>
                }
            </MudTd>
            <MudTd>
                <MudIconButton Icon="@Icons.Material.Filled.Close"
                               Size="Size.Small"
                               OnClick="@(() => RemoveFile(context))"
                               Disabled="@_isUploading" />
            </MudTd>
        </RowTemplate>
    </MudTable>

    <MudStack Row="true" Justify="Justify.FlexEnd" Class="mt-4">
        <MudButton Variant="Variant.Outlined"
                   OnClick="ClearFiles"
                   Disabled="@_isUploading">
            Limpiar
        </MudButton>
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   OnClick="UploadFiles"
                   Disabled="@_isUploading">
            Subir @_selectedFiles.Count archivo(s)
        </MudButton>
    </MudStack>
}

@code {
    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public EventCallback OnUploadComplete { get; set; }
    [Parameter] public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50MB

    private List<IBrowserFile> _selectedFiles = new();
    private Dictionary<string, string> _uploadProgress = new();
    private bool _isDragOver = false;
    private bool _isUploading = false;
    private int _uploadProgressPercent = 0;

    private void HandleDragEnter() => _isDragOver = true;
    private void HandleDragLeave() => _isDragOver = false;

    private async Task HandleDrop()
    {
        _isDragOver = false;
        // Procesar archivos dropeados
    }

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(maximumFileCount: 100))
        {
            if (file.Size > MaxFileSize)
            {
                Snackbar.Add($"Archivo {file.Name} excede el tamaño máximo", Severity.Warning);
                continue;
            }

            if (!_selectedFiles.Any(f => f.Name == file.Name))
            {
                _selectedFiles.Add(file);
            }
        }
    }

    private async Task UploadFiles()
    {
        if (!_selectedFiles.Any()) return;

        _isUploading = true;
        _uploadProgress.Clear();
        var totalFiles = _selectedFiles.Count;
        var completedFiles = 0;

        foreach (var file in _selectedFiles.ToList())
        {
            try
            {
                _uploadProgress[file.Name] = "Subiendo...";
                StateHasChanged();

                using var stream = file.OpenReadStream(MaxFileSize);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var command = new UploadDocumentCommand
                {
                    ProjectId = ProjectId,
                    FileName = file.Name,
                    ContentType = file.ContentType,
                    Content = memoryStream.ToArray()
                };

                await Mediator.Send(command);

                _uploadProgress[file.Name] = "Completado";
                completedFiles++;
                _uploadProgressPercent = (int)((completedFiles / (double)totalFiles) * 100);
            }
            catch (Exception ex)
            {
                _uploadProgress[file.Name] = "Error";
                Snackbar.Add($"Error subiendo {file.Name}: {ex.Message}", Severity.Error);
            }

            StateHasChanged();
        }

        _isUploading = false;
        Snackbar.Add($"Se subieron {completedFiles} de {totalFiles} archivos",
                     completedFiles == totalFiles ? Severity.Success : Severity.Warning);

        await OnUploadComplete.InvokeAsync();
        ClearFiles();
    }

    private void RemoveFile(IBrowserFile file) => _selectedFiles.Remove(file);
    private void ClearFiles() { _selectedFiles.Clear(); _uploadProgress.Clear(); }
    private string GetDropZoneClass() => _isDragOver ? "mud-primary-lighten" : "";
    private Color GetProgressColor(string status) => status switch
    {
        "Completado" => Color.Success,
        "Error" => Color.Error,
        _ => Color.Info
    };
    private string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
```

### 3.6 ChatWindow.razor (Chat RAG)

```razor
@inject IMediator Mediator
@inject IJSRuntime JS

<MudPaper Elevation="2" Class="d-flex flex-column" Style="height: 600px;">
    <!-- Header -->
    <MudToolBar Dense="true" Class="border-b">
        <MudIcon Icon="@Icons.Material.Filled.Chat" Class="mr-2" />
        <MudText Typo="Typo.subtitle1">Chat con Documentos</MudText>
        <MudSpacer />
        <MudIconButton Icon="@Icons.Material.Filled.DeleteSweep"
                       Size="Size.Small"
                       OnClick="ClearChat"
                       Title="Limpiar chat" />
    </MudToolBar>

    <!-- Messages -->
    <MudPaper Class="flex-grow-1 overflow-auto pa-4" Elevation="0" id="chat-messages">
        @if (!_messages.Any())
        {
            <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Class="h-100">
                <MudIcon Icon="@Icons.Material.Filled.QuestionAnswer"
                         Size="Size.Large"
                         Color="Color.Secondary" />
                <MudText Typo="Typo.body1" Color="Color.Secondary">
                    Haz una pregunta sobre tus documentos
                </MudText>
            </MudStack>
        }
        else
        {
            @foreach (var message in _messages)
            {
                <ChatMessage Message="message" OnDocumentClick="@HandleDocumentClick" />
            }

            @if (_isProcessing)
            {
                <MudStack Row="true" Class="mb-4">
                    <MudAvatar Color="Color.Primary" Size="Size.Small">
                        <MudIcon Icon="@Icons.Material.Filled.SmartToy" />
                    </MudAvatar>
                    <MudPaper Class="pa-3 ml-2" Elevation="1">
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                        <MudText Typo="Typo.body2" Class="ml-2" Style="display: inline;">
                            Pensando...
                        </MudText>
                    </MudPaper>
                </MudStack>
            }
        }
    </MudPaper>

    <!-- Input -->
    <MudPaper Class="pa-3 border-t" Elevation="0">
        <MudStack Row="true" Spacing="2">
            <MudTextField @bind-Value="_inputMessage"
                          Placeholder="Escribe tu pregunta..."
                          Variant="Variant.Outlined"
                          Immediate="true"
                          OnKeyDown="@HandleKeyDown"
                          Disabled="@_isProcessing"
                          FullWidth="true"
                          Lines="1" />
            <MudIconButton Icon="@Icons.Material.Filled.Send"
                           Color="Color.Primary"
                           Variant="Variant.Filled"
                           OnClick="SendMessage"
                           Disabled="@(string.IsNullOrWhiteSpace(_inputMessage) || _isProcessing)" />
        </MudStack>
    </MudPaper>
</MudPaper>

@code {
    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public EventCallback<Guid> OnDocumentReferenceClick { get; set; }

    private List<ChatMessageDto> _messages = new();
    private string _inputMessage = "";
    private bool _isProcessing = false;

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_inputMessage)) return;

        var userMessage = new ChatMessageDto
        {
            Role = "user",
            Content = _inputMessage,
            Timestamp = DateTime.Now
        };
        _messages.Add(userMessage);

        var question = _inputMessage;
        _inputMessage = "";
        _isProcessing = true;

        await ScrollToBottom();

        try
        {
            var response = await Mediator.Send(new ChatQuery
            {
                ProjectId = ProjectId,
                Question = question
            });

            var assistantMessage = new ChatMessageDto
            {
                Role = "assistant",
                Content = response.Answer,
                ReferencedDocuments = response.ReferencedDocuments,
                Timestamp = DateTime.Now
            };
            _messages.Add(assistantMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = new ChatMessageDto
            {
                Role = "assistant",
                Content = $"Error: {ex.Message}",
                IsError = true,
                Timestamp = DateTime.Now
            };
            _messages.Add(errorMessage);
        }
        finally
        {
            _isProcessing = false;
            await ScrollToBottom();
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private async Task HandleDocumentClick(Guid documentId)
    {
        await OnDocumentReferenceClick.InvokeAsync(documentId);
    }

    private void ClearChat() => _messages.Clear();

    private async Task ScrollToBottom()
    {
        await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
    }
}
```

### 3.7 Project Detail.razor (Página Completa)

```razor
@page "/project/{ProjectId:guid}"
@inject IMediator Mediator
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar

<PageTitle>@(_project?.Name ?? "Cargando...") - Babel</PageTitle>

@if (_isLoading)
{
    <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
}
else if (_project is null)
{
    <MudAlert Severity="Severity.Error">Proyecto no encontrado</MudAlert>
}
else
{
    <MudBreadcrumbs Items="_breadcrumbs" Class="mb-4" />

    <MudText Typo="Typo.h4" Class="mb-4">@_project.Name</MudText>

    <MudGrid>
        <!-- Chat Section (Left) -->
        <MudItem xs="12" md="6">
            <ChatWindow ProjectId="ProjectId"
                        OnDocumentReferenceClick="@ScrollToDocument" />
        </MudItem>

        <!-- Documents Section (Right) -->
        <MudItem xs="12" md="6">
            <MudStack Spacing="4">
                <!-- File Upload -->
                <MudExpansionPanels>
                    <MudExpansionPanel Text="Subir Archivos"
                                       Icon="@Icons.Material.Filled.Upload"
                                       IsInitiallyExpanded="false">
                        <FileUpload ProjectId="ProjectId"
                                    OnUploadComplete="@LoadDocuments" />
                    </MudExpansionPanel>
                </MudExpansionPanels>

                <!-- File List -->
                <FileList ProjectId="ProjectId"
                          Documents="@_documents"
                          OnDocumentSelected="@HandleDocumentSelected" />
            </MudStack>
        </MudItem>
    </MudGrid>
}

@code {
    [Parameter] public Guid ProjectId { get; set; }

    private ProjectDto? _project;
    private List<DocumentDto> _documents = new();
    private bool _isLoading = true;
    private List<BreadcrumbItem> _breadcrumbs = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadProject();
        await LoadDocuments();

        _breadcrumbs = new List<BreadcrumbItem>
        {
            new("Proyectos", href: "/"),
            new(_project?.Name ?? "Proyecto", href: null, disabled: true)
        };
    }

    private async Task LoadProject()
    {
        _isLoading = true;
        try
        {
            _project = await Mediator.Send(new GetProjectByIdQuery { Id = ProjectId });
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadDocuments()
    {
        try
        {
            var result = await Mediator.Send(new GetDocumentsByProjectQuery { ProjectId = ProjectId });
            _documents = result.ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cargando documentos: {ex.Message}", Severity.Error);
        }
    }

    private void HandleDocumentSelected(DocumentDto document)
    {
        // Abrir panel de detalles o modal
    }

    private void ScrollToDocument(Guid documentId)
    {
        // Scroll to document in list
    }
}
```

---

## 4. Convenciones de Nombrado

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Componentes | PascalCase, sufijo descriptivo | `ProjectCard.razor`, `FileUpload.razor` |
| Páginas | PascalCase, en carpeta Pages | `Index.razor`, `Detail.razor` |
| Parámetros | PascalCase, atributo `[Parameter]` | `public Guid ProjectId { get; set; }` |
| Campos privados | _camelCase | `private bool _isLoading` |
| Eventos | On + Verbo | `OnClick`, `OnUploadComplete` |
| Métodos handler | Handle + Evento | `HandleFileSelected`, `HandleKeyDown` |
| CSS classes | kebab-case | `hover-card`, `chat-messages` |

---

## 5. Integración con Backend

### Inyección de MediatR

```csharp
// En cualquier componente
@inject IMediator Mediator

// Uso en código
var projects = await Mediator.Send(new GetProjectsQuery());
await Mediator.Send(new CreateProjectCommand { Name = "Nuevo Proyecto" });
```

### Registro de Servicios en Program.cs

```csharp
// Application services (MediatR, AutoMapper, etc.)
builder.Services.AddApplicationServices();

// Infrastructure services (DbContext, Repositories, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// El DbContext ya viene registrado desde Infrastructure
// NO registrar aquí directamente
```

### Acceso a DbContext (solo si es necesario)

```csharp
// Preferir usar MediatR para queries/commands
// Si necesitas acceso directo (no recomendado):
@inject BabelDbContext DbContext

// Mejor alternativa - crear un Query
public record GetProjectStatsQuery(Guid ProjectId) : IRequest<ProjectStatsDto>;
```

---

## 6. Referencia Rápida de Componentes MudBlazor

### Layout

| Componente | Uso |
|------------|-----|
| `MudLayout` | Contenedor principal de la app |
| `MudAppBar` | Barra superior |
| `MudDrawer` | Panel lateral colapsable |
| `MudMainContent` | Área de contenido principal |
| `MudContainer` | Contenedor con max-width |

### Navegación

| Componente | Uso |
|------------|-----|
| `MudNavMenu` | Menú de navegación |
| `MudNavLink` | Link de navegación |
| `MudBreadcrumbs` | Migas de pan |
| `MudTabs` | Pestañas |

### Inputs

| Componente | Uso |
|------------|-----|
| `MudTextField` | Campo de texto |
| `MudSelect` | Dropdown |
| `MudAutocomplete` | Autocompletado |
| `MudDatePicker` | Selector de fecha |
| `MudFileUpload` | Upload de archivos |

### Data Display

| Componente | Uso |
|------------|-----|
| `MudDataGrid` | Tabla con ordenación/filtrado |
| `MudTable` | Tabla simple |
| `MudList` | Lista de items |
| `MudTreeView` | Vista de árbol |

### Feedback

| Componente | Uso |
|------------|-----|
| `MudAlert` | Mensaje de alerta |
| `MudSnackbar` | Notificación toast |
| `MudDialog` | Modal dialog |
| `MudProgress*` | Indicadores de progreso |
| `MudSkeleton` | Placeholder de carga |

### Surfaces

| Componente | Uso |
|------------|-----|
| `MudCard` | Tarjeta con contenido |
| `MudPaper` | Superficie elevada |
| `MudExpansionPanel` | Panel colapsable |

---

## 7. Troubleshooting Común

### Error: MudBlazor styles not loading

**Solución:** Verificar que `_Host.cshtml` incluye:
```html
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

### Error: MudDialogProvider not found

**Solución:** Agregar providers en `App.razor`:
```razor
<MudThemeProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

### Error: IMediator not registered

**Solución:** Verificar registro en `Program.cs`:
```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetProjectsQuery).Assembly));
```

### Error: Component not updating

**Solución:** Llamar `StateHasChanged()` o usar `InvokeAsync`:
```csharp
await InvokeAsync(StateHasChanged);
```

### Error: JavaScript interop failed

**Solución:** Para funciones JS personalizadas, agregar en `_Host.cshtml`:
```html
<script>
    window.scrollToBottom = (elementId) => {
        var element = document.getElementById(elementId);
        if (element) element.scrollTop = element.scrollHeight;
    };
</script>
```

### MudDataGrid no muestra datos

**Verificar:**
1. `Items` está correctamente bindeado
2. Los datos no son `null`
3. Las columnas tienen `Property` correcto

### Estilos CSS personalizados no se aplican

**Solución:** Usar CSS isolation:
```
Component.razor
Component.razor.css  <- Archivo CSS con mismo nombre
```

---

## 8. Best Practices para Babel

1. **Usar MediatR para todo acceso a datos** - Evita acoplar componentes a DbContext
2. **Componentes pequeños y reutilizables** - Un componente = una responsabilidad
3. **Evitar lógica en .razor** - Mover lógica compleja a services o handlers
4. **Manejar estados de carga** - Siempre mostrar skeleton/progress durante cargas
5. **Feedback al usuario** - Usar Snackbar para notificar éxito/error
6. **Validación en Forms** - Usar `EditForm` con `DataAnnotationsValidator`
7. **Async todo** - Todas las operaciones I/O deben ser async
8. **Dispose de recursos** - Implementar `IDisposable` si hay suscripciones
