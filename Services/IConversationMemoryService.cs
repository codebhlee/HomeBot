using Microsoft.SemanticKernel.ChatCompletion;

namespace HomeBot.Services;

public interface IConversationMemoryService
{
    ChatHistory GetOrCreateHistory(long chatId);

    Task<ChatHistory> BuildContextualHistoryAsync(
        long chatId, string userMessage, string modelName,
        string? customSystemPrompt, CancellationToken ct);

    Task SaveToMemoryAsync(
        long chatId, string userMessage, string assistantResponse, CancellationToken ct);

    void AddAssistantResponse(long chatId, string modelName, string response);
}
