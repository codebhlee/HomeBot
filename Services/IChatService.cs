using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeBot.Services;

public interface IChatService
{
    /// <summary>
    /// SK ChatCompletion + Memory를 사용하여 스트리밍 응답을 Telegram으로 전송
    /// </summary>
    Task<string> RespondAsync(
        ITelegramBotClient bot,
        long chatId,
        string modelName,
        string userPrompt,
        float temperature,
        int numPredict,
        string? customSystemPrompt,
        CancellationToken ct);
}
