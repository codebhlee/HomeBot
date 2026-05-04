using HomeBot.Extensions;
using HomeBot.Models;
using HomeBot.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HomeBot.Factories;

/// <summary>
/// Telegram Message + Session 정보를 바탕으로 적절한 AIContext를 생성하는 팩토리.
/// 메시지 타입(텍스트/사진)과 현재 모드에 따른 도메인 판단 로직을 캡슐화.
/// </summary>
public sealed class AIContextFactory
{
    private readonly ISessionService _sessionService;

    public AIContextFactory(ISessionService sessionService)
        => _sessionService = sessionService;

    /// <summary>
    /// 메시지와 세션 상태를 분석하여 큐에 넣을 AIContext를 생성.
    /// null 반환 시 처리 불필요 (빈 메시지 등).
    /// </summary>
    public async Task<AIContext?> CreateAsync(Message message, CancellationToken ct)
    {
        var chatId   = message.Chat.Id;
        var session  = await _sessionService.GetOrCreateAsync(chatId, ct);
        var model    = session.SessionSettings.CurrentMode;
        var prompts  = session.SessionSettings.CustomSystemPrompts;

        // 사진 메시지 — 이미지 모드면 img2img, 아니면 Llama로 설명
        if (message.Type == MessageType.Photo)
        {
            prompts.TryGetValue(AIModelType.Llama31Instruct.ToModelName(), out var photoPrompt);
            return CreateFromPhoto(chatId, message.Caption, model, session.SessionSettings, photoPrompt);
        }

        // 텍스트 메시지
        var text = message.Text;
        if (string.IsNullOrEmpty(text)) return null;

        prompts.TryGetValue(model.ToModelName(), out var customSystemPrompt);
        return CreateFromText(chatId, text, model, session.SessionSettings, customSystemPrompt);
    }

    private static AIContext CreateFromPhoto(
        long chatId, string? caption, AIModelType model, SessionSettings settings,
        string? llamaSystemPrompt)
    {
        // 이미지 모드에서 사진 → img2img (TODO: img2img 구현 예정)
        if (model.IsImageModel())
            return AIModelType.StableDiffusion.ToImageContext(chatId, caption ?? string.Empty);

        // 그 외 모드에서 사진 → Llama로 이미지 설명 (Llama 커스텀 프롬프트 적용)
        return AIModelType.Llama31Instruct.ToChatContext(
            chatId, caption ?? "이 이미지를 설명해줘.", customSystemPrompt: llamaSystemPrompt);
    }

    private static AIContext CreateFromText(
        long chatId, string text, AIModelType model, SessionSettings settings,
        string? customSystemPrompt)
    {
        // 이미지 모드에서 텍스트 → 이미지 생성
        if (model.IsImageModel())
            return AIModelType.StableDiffusion.ToImageContext(chatId, text);

        // Chat 모드 — 세션 설정 기반 파라미터 + 커스텀 프롬프트 적용
        return model.ToChatContext(
            chatId,
            text,
            customSystemPrompt: customSystemPrompt,
            temperature:        settings.Temperature,
            numPredict:         settings.MaxTokens ?? model.DefaultNumPredict());
    }
}
