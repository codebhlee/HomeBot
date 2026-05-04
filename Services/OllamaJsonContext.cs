using System.Text.Json.Serialization;
using HomeBot.Models;

namespace HomeBot.Services;

[JsonSerializable(typeof(OllamaRequest))]
[JsonSerializable(typeof(OllamaChunk))]
[JsonSerializable(typeof(OllamaChatRequest))]
[JsonSerializable(typeof(OllamaChatChunk))]
[JsonSerializable(typeof(OllamaChatMessage))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(SdRequest))]
[JsonSerializable(typeof(SdResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class OllamaJsonContext : JsonSerializerContext { }
