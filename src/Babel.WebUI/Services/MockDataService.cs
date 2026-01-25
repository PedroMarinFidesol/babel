using Babel.Application.DTOs;
using Babel.Domain.Enums;

namespace Babel.WebUI.Services;

public class MockDataService
{
    private readonly List<ProjectDto> _projects;
    private readonly Dictionary<Guid, List<DocumentDto>> _documents;
    private readonly Dictionary<Guid, List<ChatMessageDto>> _chatHistory;

    public MockDataService()
    {
        // Initialize mock projects
        _projects =
        [
            new ProjectDto
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Facturas 2024",
                Description = "Facturas de proveedores y clientes del ano 2024",
                TotalDocuments = 45,
                ProcessedDocuments = 42,
                PendingDocuments = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new ProjectDto
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Contratos Laborales",
                Description = "Contratos de empleados y colaboradores",
                TotalDocuments = 28,
                ProcessedDocuments = 28,
                PendingDocuments = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new ProjectDto
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Manuales Tecnicos",
                Description = "Documentacion tecnica de productos y sistemas",
                TotalDocuments = 12,
                ProcessedDocuments = 8,
                PendingDocuments = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            }
        ];

        // Initialize mock documents
        _documents = new Dictionary<Guid, List<DocumentDto>>
        {
            [_projects[0].Id] = GenerateMockDocuments(_projects[0].Id, ["factura_001.pdf", "factura_002.pdf", "factura_003.png", "resumen_mensual.xlsx"]),
            [_projects[1].Id] = GenerateMockDocuments(_projects[1].Id, ["contrato_juan.pdf", "contrato_maria.pdf", "anexo_confidencial.docx"]),
            [_projects[2].Id] = GenerateMockDocuments(_projects[2].Id, ["manual_api.pdf", "guia_usuario.pdf", "diagrama_arquitectura.png"])
        };

        // Initialize empty chat histories
        _chatHistory = new Dictionary<Guid, List<ChatMessageDto>>();
    }

    public Task<List<ProjectDto>> GetProjectsAsync()
    {
        return Task.FromResult(_projects.ToList());
    }

    public Task<ProjectDto?> GetProjectByIdAsync(Guid id)
    {
        return Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));
    }

    public Task<List<DocumentDto>> GetDocumentsByProjectIdAsync(Guid projectId)
    {
        return Task.FromResult(_documents.TryGetValue(projectId, out var docs) ? docs.ToList() : []);
    }

    public Task<List<ChatMessageDto>> GetChatHistoryAsync(Guid projectId)
    {
        return Task.FromResult(_chatHistory.TryGetValue(projectId, out var history) ? history.ToList() : []);
    }

    public async Task<ChatMessageDto> SendChatMessageAsync(Guid projectId, string message)
    {
        // Ensure chat history exists for project
        if (!_chatHistory.ContainsKey(projectId))
        {
            _chatHistory[projectId] = [];
        }

        // Add user message
        var userMessage = new ChatMessageDto
        {
            Content = message,
            IsUser = true,
            Timestamp = DateTime.UtcNow
        };
        _chatHistory[projectId].Add(userMessage);

        // Simulate processing delay
        await Task.Delay(800);

        // Generate mock response
        var response = GenerateMockResponse(projectId, message);
        _chatHistory[projectId].Add(response);

        return response;
    }

    private ChatMessageDto GenerateMockResponse(Guid projectId, string userMessage)
    {
        var documents = _documents.TryGetValue(projectId, out var docs) ? docs : [];
        var relevantDocs = documents.Take(2).ToList();

        var responses = new[]
        {
            "Basandome en los documentos del proyecto, puedo indicarte que ",
            "Segun la informacion disponible en los archivos procesados, ",
            "He analizado los documentos relevantes y encontre que ",
            "De acuerdo con los datos extraidos del proyecto, "
        };

        var details = new[]
        {
            "la informacion solicitada se encuentra en los documentos referenciados.",
            "existen varios registros relacionados con tu consulta.",
            "los datos muestran patrones consistentes con lo que mencionas.",
            "he identificado secciones relevantes en los documentos adjuntos."
        };

        var random = new Random();
        var responseText = responses[random.Next(responses.Length)] + details[random.Next(details.Length)];

        return new ChatMessageDto
        {
            Content = responseText,
            IsUser = false,
            Timestamp = DateTime.UtcNow,
            References = relevantDocs.Select(d => new DocumentReferenceDto
            {
                DocumentId = d.Id,
                FileName = d.FileName,
                Snippet = "...fragmento relevante del documento...",
                RelevanceScore = 0.75 + random.NextDouble() * 0.25
            }).ToList()
        };
    }

    private static List<DocumentDto> GenerateMockDocuments(Guid projectId, string[] fileNames)
    {
        var random = new Random();
        var statuses = new[] { DocumentStatus.Completed, DocumentStatus.Completed, DocumentStatus.Completed, DocumentStatus.Processing, DocumentStatus.Pending };

        return fileNames.Select((name, index) =>
        {
            var extension = Path.GetExtension(name);
            var status = statuses[random.Next(statuses.Length)];
            return new DocumentDto
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                FileName = name,
                FileExtension = extension,
                FileSizeBytes = random.Next(50000, 5000000),
                MimeType = GetMimeType(extension),
                Status = status,
                IsVectorized = status == DocumentStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                ProcessedAt = status == DocumentStatus.Completed ? DateTime.UtcNow.AddDays(-random.Next(1, 10)) : null
            };
        }).ToList();
    }

    private static string GetMimeType(string extension) => extension.ToLower() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
