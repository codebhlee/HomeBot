using HomeBot.Models;
using HomeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeBot.Commands;

/// <summary>/gen — 일반 대화 모드(Llama31Instruct)로 전환</summary>
public sealed class GenCommand : IBotCommand
{
    private readonly ISessionService _sessionService;

    public string Command => "/gen";

    public GenCommand(ISessionService sessionService)
        => _sessionService = sessionService;

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var session = await _sessionService.GetOrCreateAsync(message.Chat.Id, ct);
        session.SessionSettings.CurrentMode = AIModelType.Llama31Instruct;
        await _sessionService.SaveAsync(session, ct);

        await bot.SendMessage(message.Chat.Id,
            "일반 모드(llama3.1 8b instruct)로 전환되었습니다. 🦙",
            cancellationToken: ct);
    }
}
