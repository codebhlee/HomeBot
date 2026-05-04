using System.Text.Json.Serialization;

namespace HomeBot.Models;

internal sealed record OllamaOptions(
    [property: JsonPropertyName("num_thread")]  int    NumThread,
    [property: JsonPropertyName("num_gpu")]     int    NumGpu,
    [property: JsonPropertyName("low_vram")]    bool   LowVram,
    [property: JsonPropertyName("temperature")] double Temperature);

internal sealed record OllamaRequest(
    [property: JsonPropertyName("model")]   string        Model,
    [property: JsonPropertyName("prompt")]  string        Prompt,
    [property: JsonPropertyName("stream")]  bool          Stream,
    [property: JsonPropertyName("options")] OllamaOptions Options);

internal sealed record OllamaChunk(
    [property: JsonPropertyName("response")] string? Response,
    [property: JsonPropertyName("done")]     bool    Done);

// ── /api/chat 전용 타입 ───────────────────────────────────────────────────────
internal sealed record OllamaChatOptions(
    [property: JsonPropertyName("num_thread")]  int    NumThread,
    [property: JsonPropertyName("num_gpu")]     int    NumGpu,
    [property: JsonPropertyName("low_vram")]    bool   LowVram,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("num_predict")] int    NumPredict,
    [property: JsonPropertyName("num_ctx")]     int    NumCtx = 4096);

internal sealed record OllamaChatMessage(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record OllamaChatRequest(
    [property: JsonPropertyName("model")]    string              Model,
    [property: JsonPropertyName("messages")] OllamaChatMessage[] Messages,
    [property: JsonPropertyName("stream")]   bool                Stream,
    [property: JsonPropertyName("options")]  OllamaChatOptions   Options);

internal sealed record OllamaChatChunk(
    [property: JsonPropertyName("message")] OllamaChatMessage? Message,
    [property: JsonPropertyName("done")]    bool               Done);
