using System.Text;
using HomeBot.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

#pragma warning disable SKEXP0001, SKEXP0070
namespace HomeBot.Services;

/// <summary>
/// SK Kernel 기반 대화 서비스.
/// - Function Calling: WeatherPlugin 등 등록된 플러그인 자동 호출
/// - 스트리밍: SK IAsyncEnumerable 스트리밍으로 Telegram 실시간 업데이트
/// - ConversationMemory: (chatId, modelName) 키 기반 히스토리 관리
/// </summary>
internal sealed class ChatService : IChatService
{
    private const double EditIntervalSec = 0.8;

    private readonly Kernel                       _kernel;
    private readonly IConversationMemoryService   _memory;
    private readonly ILogger<ChatService>         _logger;

    public ChatService(Kernel kernel, IConversationMemoryService memory, ILogger<ChatService> logger)
    {
        _kernel = kernel;
        _memory = memory;
        _logger = logger;
    }

    public async Task<string> RespondAsync(
        ITelegramBotClient bot,
        long chatId,
        string modelName,
        string userPrompt,
        float temperature,
        int numPredict,
        string? customSystemPrompt,
        CancellationToken ct)
    {
        _logger.LogInformation("Chat 요청 | chatId:{ChatId} model:{Model} temp:{Temp} predict:{Predict} | {Prompt}",
            chatId, modelName, temperature, numPredict, userPrompt);

        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        // ChatHistory + TextMemory 컨텍스트 구성
        var history = await _memory.BuildContextualHistoryAsync(
            chatId, userPrompt, modelName, customSystemPrompt, ct);

        // SK 실행 설정 — Function Calling 활성화 + Ollama 파라미터
        var executionSettings = new PromptExecutionSettings
        {
            ModelId                = modelName,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ExtensionData          = new Dictionary<string, object>
            {
                ["temperature"] = (double)temperature,
                ["num_predict"] = numPredict,
                ["num_ctx"]     = 2048,
                ["num_thread"]  = 8,
                ["num_gpu"]     = 99,
            },
        };

        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        try
        {
            // non-streaming으로 먼저 응답 받기 (Function Calling 포함)
            var results = await chatCompletion.GetChatMessageContentsAsync(
                history, executionSettings, _kernel, ct);

            var finalAnswer = string.Concat(results.Select(r => r.Content ?? string.Empty));

            _logger.LogInformation("응답 수신 | chatId:{ChatId} length:{Len}", chatId, finalAnswer.Length);

            if (string.IsNullOrWhiteSpace(finalAnswer))
            {
                _logger.LogWarning("빈 응답 수신 | chatId:{ChatId}", chatId);
                await bot.SendMessage(chatId, "답변을 생성하지 못했습니다.", cancellationToken: ct);
                return string.Empty;
            }

            await bot.SendMessage(chatId, finalAnswer, cancellationToken: ct);

            // ChatHistory + TextMemory에 저장
            _memory.AddAssistantResponse(chatId, modelName, finalAnswer);
            await _memory.SaveToMemoryAsync(chatId, userPrompt, finalAnswer, ct);

            return finalAnswer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "응답 오류 | chatId:{ChatId} model:{Model}", chatId, modelName);
            await bot.SendMessage(chatId, $"오류가 발생했습니다: {ex.Message}", cancellationToken: ct);
            return string.Empty;
        }
    }
}
