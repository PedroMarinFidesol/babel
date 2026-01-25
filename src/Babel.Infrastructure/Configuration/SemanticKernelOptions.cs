using System.ComponentModel.DataAnnotations;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para Semantic Kernel.
/// </summary>
public class SemanticKernelOptions
{
    public const string SectionName = "SemanticKernel";

    [Required]
    public string DefaultProvider { get; set; } = "Ollama";

    public OllamaOptions Ollama { get; set; } = new();
    public OpenAIOptions OpenAI { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
}

public class OllamaOptions
{
    [Url]
    public string Endpoint { get; set; } = "http://localhost:11434";

    public string ChatModel { get; set; } = "llama2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4";
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
}

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gemini-pro";
}
