using HomeBot.Models;

namespace HomeBot.Extensions;

public static class AIContextExtensions
{
    public static AIContext ToChatContext(
        this AIModelType model, long chatId, string prompt,
        string? customSystemPrompt = null,
        float?  temperature        = null,
        int?    numPredict         = null)
        => new(chatId, AITaskType.Chat, model, prompt,
               temperature ?? model.DefaultTemperature(),
               numPredict  ?? model.DefaultNumPredict(),
               customSystemPrompt);

    public static AIContext ToImageContext(this AIModelType model, long chatId, string prompt)
        => new(chatId, AITaskType.Image, model, prompt);
}
