using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeBot.Commands;

/// <summary>
/// Telegram 봇 명령어 전략 인터페이스
/// </summary>
public interface IBotCommand
{
    /// <summary>처리할 명령어 (예: "/code")</summary>
    string Command { get; }

    Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct);
}
