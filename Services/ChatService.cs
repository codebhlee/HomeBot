using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HomeBot.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

#pragma warning disable SKEXP0001

namespace HomeBot.Services;

/// <summary>
/// Ollama /api/chat 직접 호출 + ConversationMemory 기반 대화 서비스
/// num_predict 등 SK가 노출하지 않는 파라미터를 직접 제어
/// </summary>
internal sealed class ChatService : IChatService
{
    private const int    WordThreshold   = 7;
    private const double EditIntervalSec = 0.8;

    private readonly Uri                          _ollamaEndpoint;
    private readonly IHttpClientFactory           _httpFactory;
    private readonly IConversationMemoryService   _memory;
    private readonly ILogger<ChatService>         _logger;

    public ChatService(
        Uri ollamaEndpoint,
        IHttpClientFactory httpFactory,
        IConversationMemoryService memory,
        ILogger<ChatService> logger)
    {
        _ollamaEndpoint = ollamaEndpoint;
        _httpFactory    = httpFactory;
        _memory         = memory;
        _logger         = logger;
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
        var history = await _memory.BuildContextualHistoryAsync(chatId, userPrompt, modelName, customSystemPrompt, ct);

        // ChatHistory → OllamaChatMessage 배열 변환
        var messages = history
            .Select(h => new OllamaChatMessage(h.Role.Label, h.Content ?? string.Empty))
            .ToArray();

        var options = new OllamaChatOptions(
            NumThread:   8,     // M 시리즈는 효율 코어 포함 8~10개
            NumGpu:      99,    // Apple Silicon: 전체 레이어를 Metal GPU에 올림 (-1 또는 큰 값)
            LowVram:     false,
            Temperature: temperature,
            NumPredict:  numPredict,
            NumCtx:      2048);

        var request = new OllamaChatRequest(modelName, messages, Stream: true, options);
        var body    = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaChatRequest);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var http     = _httpFactory.CreateClient("ollama");
        var response = await http.PostAsync("/api/chat", content, ct);
        response.EnsureSuccessStatusCode();

        // 스트리밍 응답 처리
        var answerBuilder = new StringBuilder();
        Message? sentMessage  = null;
        int      lastSentLen  = 0;
        var      lastEditTime = DateTime.MinValue;

        await foreach (var chunk in StreamChatAsync(response, ct))
        {
            if (!string.IsNullOrEmpty(chunk.Message?.Content))
                answerBuilder.Append(chunk.Message.Content);

            var len    = answerBuilder.Length;
            var hasNew = len > lastSentLen;

            if (sentMessage == null)
            {
                if (hasNew && answerBuilder.ToString().Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= WordThreshold)
                {
                    lastSentLen  = len;
                    sentMessage  = await bot.SendMessage(chatId, answerBuilder.ToString(), cancellationToken: ct);
                    lastEditTime = DateTime.UtcNow;
                }
            }
            else if (hasNew && (DateTime.UtcNow - lastEditTime).TotalSeconds >= EditIntervalSec)
            {
                lastSentLen = len;
                try
                {
                    await bot.EditMessageText(chatId, sentMessage.MessageId, answerBuilder.ToString(), cancellationToken: ct);
                    lastEditTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "EditMessage 실패 무시 | chatId:{ChatId}", chatId);
                }
            }

            if (chunk.Done) break;
        }

        var finalAnswer = answerBuilder.ToString();

        if (sentMessage != null && answerBuilder.Length > lastSentLen)
        {
            try { await bot.EditMessageText(chatId, sentMessage.MessageId, finalAnswer, cancellationToken: ct); }
            catch { /* 무시 */ }
        }
        else if (sentMessage == null)
        {
            await bot.SendMessage(chatId,
                string.IsNullOrEmpty(finalAnswer) ? "답변을 생성하지 못했습니다." : finalAnswer,
                cancellationToken: ct);
        }

        // ChatHistory + TextMemory에 저장
        _memory.AddAssistantResponse(chatId, modelName, finalAnswer);
        await _memory.SaveToMemoryAsync(chatId, userPrompt, finalAnswer, ct);

        return finalAnswer;
    }

    // PipeReader 기반 스트리밍 파서
    private static async IAsyncEnumerable<OllamaChatChunk> StreamChatAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        var pipe = PipeReader.Create(stream);

        try
        {
            while (true)
            {
                var result = await pipe.ReadAsync(ct);
                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    var chunk = ParseChatChunk(line);
                    if (chunk is not null) yield return chunk;
                }

                pipe.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await pipe.CompleteAsync();
        }
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(out line, (byte)'\n'))
        {
            buffer = buffer.Slice(reader.Position);
            return true;
        }
        line = default;
        return false;
    }

    private static OllamaChatChunk? ParseChatChunk(ReadOnlySequence<byte> utf8Json)
    {
        if (utf8Json.IsEmpty) return null;

        string? role    = null;
        string? content = null;
        bool    done    = false;

        var reader = new Utf8JsonReader(utf8Json);
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueTextEquals("done"u8))
            {
                reader.Read();
                done = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals("message"u8))
            {
                reader.Read(); // StartObject
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    if (reader.ValueTextEquals("role"u8))    { reader.Read(); role    = reader.GetString(); }
                    else if (reader.ValueTextEquals("content"u8)) { reader.Read(); content = reader.GetString(); }
                    else reader.Skip();
                }
            }
            else
            {
                reader.Skip();
            }
        }

        var msg = (role != null || content != null)
            ? new OllamaChatMessage(role ?? "assistant", content ?? string.Empty)
            : null;

        return new OllamaChatChunk(msg, done);
    }
}
