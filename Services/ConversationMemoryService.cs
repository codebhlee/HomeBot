using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0001, SKEXP0050

namespace HomeBot.Services;

/// <summary>
/// 사용자별 ChatHistory 관리 + TextMemory 기반 과거 대화 자동 조회/저장
/// </summary>
internal sealed class ConversationMemoryService : IConversationMemoryService
{
    private const string MemoryCollection = "chat_history";
    private const int    MaxMemoryResults = 3;
    private const int    MaxHistoryTurns  = 20;

    private readonly ISemanticTextMemory              _memory;
    private readonly ILogger<ConversationMemoryService> _logger;

    private readonly ConcurrentDictionary<(long ChatId, string ModelName), ChatHistory> _histories = new();

    public ConversationMemoryService(ISemanticTextMemory memory, ILogger<ConversationMemoryService> logger)
    {
        _memory = memory;
        _logger = logger;
    }

    // 인터페이스 호환용 — modelName 없이 호출 시 빈 문자열 키 사용
    public ChatHistory GetOrCreateHistory(long chatId)
        => _histories.GetOrAdd((chatId, string.Empty), _ => new ChatHistory());

    private ChatHistory GetOrCreateHistory(long chatId, string modelName)
        => _histories.GetOrAdd((chatId, modelName), _ => new ChatHistory());

    /// <summary>
    /// 새 질문에 대해 TextMemory에서 유사 과거 대화를 조회하여
    /// ChatHistory 앞부분에 컨텍스트로 주입한 뒤 반환.
    /// customSystemPrompt가 있으면 메모리 컨텍스트보다 우선하여 시스템 메시지로 설정.
    /// </summary>
    public async Task<ChatHistory> BuildContextualHistoryAsync(
        long chatId, string userMessage, string modelName,
        string? customSystemPrompt, CancellationToken ct)
    {
        var history = GetOrCreateHistory(chatId, modelName);

        // TextMemory에서 유사 과거 대화 조회
        var memories = new List<string>();
        try
        {
            await foreach (var result in _memory.SearchAsync(
                MemoryCollection,
                userMessage,
                limit: MaxMemoryResults,
                minRelevanceScore: 0.6, // 코싸인 유사도 : 0.8~0.9 매우 엄격. 0.6~0.7 적정수준, 0.4~0.5 느슨함(할루시네이션원인)
                cancellationToken: ct))
            {
                memories.Add(result.Metadata.Text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory 조회 오류 | chatId:{ChatId}", chatId);
        }

        // 시스템 메시지 결정: 커스텀 프롬프트 우선, 없으면 메모리 컨텍스트
        string? systemMsg = null;

        if (!string.IsNullOrWhiteSpace(customSystemPrompt))
        {
            // 커스텀 프롬프트 + 메모리 컨텍스트 병합
            if (memories.Count > 0)
            {
                var memoryContext = string.Join("\n---\n", memories);
                systemMsg = $"{customSystemPrompt}\n\n다음은 이 사용자와의 과거 관련 대화입니다. 참고하여 답변하세요:\n{memoryContext}";
            }
            else
            {
                systemMsg = customSystemPrompt;
            }
            _logger.LogInformation("Memory 커스텀 시스템 프롬프트 적용 | chatId:{ChatId}", chatId);
        }
        else if (memories.Count > 0)
        {
            var memoryContext = string.Join("\n---\n", memories);
            systemMsg = $"다음은 이 사용자와의 과거 관련 대화입니다. 참고하여 답변하세요:\n{memoryContext}";
            _logger.LogInformation("Memory {Count}개 과거 대화 컨텍스트 주입 | chatId:{ChatId}", memories.Count, chatId);
        }

        // 시스템 메시지 주입: 있으면 교체/삽입, 없으면 기존 시스템 메시지 제거
        var hasExistingSystem = history.Count > 0 && history[0].Role == AuthorRole.System;
        if (systemMsg is not null)
        {
            if (hasExistingSystem)
                history[0] = new ChatMessageContent(AuthorRole.System, systemMsg);
            else
                history.Insert(0, new ChatMessageContent(AuthorRole.System, systemMsg));
        }
        else if (hasExistingSystem)
        {
            // DB에서 커스텀 프롬프트가 삭제된 경우 시스템 메시지도 제거
            history.RemoveAt(0);
        }

        // 사용자 메시지 추가
        history.AddUserMessage(userMessage);

        // 히스토리가 너무 길면 오래된 것부터 제거 (시스템 메시지 제외)
        TrimHistory(history);

        return history;
    }

    /// <summary>
    /// 응답 완료 후 대화 쌍을 TextMemory에 저장
    /// </summary>
    public async Task SaveToMemoryAsync(
        long chatId, string userMessage, string assistantResponse, CancellationToken ct)
    {
        try
        {
            var memoryText = $"사용자: {userMessage}\n어시스턴트: {assistantResponse}";
            var memoryId   = $"{chatId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await _memory.SaveInformationAsync(
                MemoryCollection,
                text: memoryText,
                id: memoryId,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory 저장 오류 | chatId:{ChatId}", chatId);
        }
    }

    /// <summary>
    /// 어시스턴트 응답을 ChatHistory에 추가
    /// </summary>
    public void AddAssistantResponse(long chatId, string modelName, string response)
    {
        var history = GetOrCreateHistory(chatId, modelName);
        history.AddAssistantMessage(response);
        TrimHistory(history);
    }

    private static void TrimHistory(ChatHistory history)
    {
        // 시스템 메시지(index 0)는 유지하고 나머지가 MaxHistoryTurns * 2를 초과하면 제거
        int systemOffset = history.Count > 0 && history[0].Role == AuthorRole.System ? 1 : 0;
        int maxMessages  = MaxHistoryTurns * 2 + systemOffset;

        while (history.Count > maxMessages)
            history.RemoveAt(systemOffset); // 가장 오래된 user/assistant 메시지 제거
    }
}
