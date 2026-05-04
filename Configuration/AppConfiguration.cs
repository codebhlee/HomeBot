using System.Text.Json;
using HomeBot.Models;
using HomeBot.Services;

namespace HomeBot.Configuration;

public static class AppConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        // 1. 환경변수 우선 읽기
        var settings = new AppSettings
        {
            TelegramToken        = Environment.GetEnvironmentVariable("TELEGRAM_AI_HOME_BOT_TOKEN"),
            OllamaBaseUrl        = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL"),
            OllamaEmbeddingModel = "nomic-embed-text",
        };

        // 2. appsettings.json 항상 읽어서 병합 (환경변수 없는 항목만 채움)
        const string configPath = "appsettings.json";
        if (File.Exists(configPath))
        {
            var bytes  = await File.ReadAllBytesAsync(configPath, ct);
            var config = JsonSerializer.Deserialize<AppSettings>(bytes, JsonOptions);

            if (config != null)
            {
                if (string.IsNullOrEmpty(settings.TelegramToken))
                {
                    settings.TelegramToken = config.TelegramToken;
                    Console.WriteLine("[설정] Telegram 토큰: appsettings.json");
                }
                else Console.WriteLine("[설정] Telegram 토큰: 환경변수");

                if (string.IsNullOrEmpty(settings.OllamaBaseUrl))
                {
                    settings.OllamaBaseUrl = string.IsNullOrEmpty(config.OllamaBaseUrl)
                        ? "http://localhost:11434"
                        : config.OllamaBaseUrl;
                    Console.WriteLine($"[설정] Ollama 주소: appsettings.json ({settings.OllamaBaseUrl})");
                }
                else Console.WriteLine($"[설정] Ollama 주소: 환경변수 ({settings.OllamaBaseUrl})");

                if (!string.IsNullOrEmpty(config.OllamaEmbeddingModel))
                    settings.OllamaEmbeddingModel = config.OllamaEmbeddingModel;

                settings.StableDiffusionEndpoints = config.StableDiffusionEndpoints ?? [];
                Console.WriteLine($"[설정] SD 엔드포인트: {settings.StableDiffusionEndpoints.Length}개");
                Console.WriteLine($"[설정] 임베딩 모델: {settings.OllamaEmbeddingModel}");
            }
        }
        else
        {
            Console.WriteLine("[설정] appsettings.json 없음 — 환경변수만 사용");
        }

        // 3. 필수값 검증
        if (string.IsNullOrEmpty(settings.TelegramToken))
            throw new InvalidOperationException("오류: TELEGRAM_AI_HOME_BOT_TOKEN이 환경변수와 appsettings.json 어디에도 없습니다.");

        settings.OllamaBaseUrl ??= "http://localhost:11434";

        return settings;
    }
}
