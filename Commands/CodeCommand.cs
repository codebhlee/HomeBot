using HomeBot.Models;
using HomeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeBot.Commands;

/// <summary>/code — 코딩 모드(QwenCoder)로 전환</summary>
public sealed class CodeCommand : IBotCommand
{
    private readonly ISessionService _sessionService;

    public string Command => "/code";

    public CodeCommand(ISessionService sessionService)
        => _sessionService = sessionService;

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var session = await _sessionService.GetOrCreateAsync(message.Chat.Id, ct);
        session.SessionSettings.CurrentMode = AIModelType.QwenCoder;
        await _sessionService.SaveAsync(session, ct);

        await bot.SendMessage(message.Chat.Id,
            "코딩 모드(qwen2.5-coder)로 전환되었습니다. 💻",
            cancellationToken: ct);
    }
}
