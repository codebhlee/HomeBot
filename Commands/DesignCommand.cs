using HomeBot.Extensions;
using HomeBot.Models;
using HomeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeBot.Commands;

/// <summary>
/// /design — 이미지 생성 모드(StableDiffusion)로 전환.
/// 인라인 프롬프트가 있으면 즉시 이미지 생성 큐에 추가.
/// </summary>
public sealed class DesignCommand : IBotCommand
{
    private readonly ISessionService _sessionService;
    private readonly ITaskQueue      _taskQueue;

    public string Command => "/design";

    public DesignCommand(ISessionService sessionService, ITaskQueue taskQueue)
    {
        _sessionService = sessionService;
        _taskQueue      = taskQueue;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;

        var session = await _sessionService.GetOrCreateAsync(chatId, ct);
        session.SessionSettings.CurrentMode = AIModelType.StableDiffusion;
        await _sessionService.SaveAsync(session, ct);

        // "/design" 이후 인라인 프롬프트 추출
        var inlinePrompt = (message.Text ?? string.Empty)
            .Substring(Command.Length)
            .Trim();

        if (!string.IsNullOrEmpty(inlinePrompt))
        {
            await _taskQueue.EnqueueAsync(
                AIModelType.StableDiffusion.ToImageContext(chatId, inlinePrompt), ct);
        }
        else
        {
            await bot.SendMessage(chatId,
                "이미지 생성 모드(Stable Diffusion)로 전환되었습니다. 🎨\n" +
                "프롬프트를 입력하세요.\n예) a beautiful sunset --size 768x512 --steps 30",
                cancellationToken: ct);
        }
    }
}
