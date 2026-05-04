using System.Text.Json.Serialization;

namespace HomeBot.Models;

public sealed class AppSettings
{
    [JsonPropertyName("TELEGRAM_AI_HOME_BOT_TOKEN")] public string?   TelegramToken            { get; set; }
    [JsonPropertyName("OLLAMA_BASE_URL")]             public string?   OllamaBaseUrl            { get; set; }
    [JsonPropertyName("OLLAMA_EMBEDDING_MODEL")]      public string?   OllamaEmbeddingModel     { get; set; }
    [JsonPropertyName("STABLE_DIFFUSION_ENDPOINTS")] public string[]? StableDiffusionEndpoints { get; set; }
    [JsonPropertyName("POSTGRES_CONNECTION_STRING")] public string?   PostgresConnectionString { get; set; }
}
