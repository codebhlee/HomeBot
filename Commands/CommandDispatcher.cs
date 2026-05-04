using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeBot.Commands;

/// <summary>
/// 수신된 명령어를 등록된 IBotCommand 구현체로 라우팅하는 디스패처
/// </summary>
public sealed class CommandDispatcher
{
    private readonly IReadOnlyDictionary<string, IBotCommand> _commands;

    public CommandDispatcher(IEnumerable<IBotCommand> commands)
        => _commands = commands.ToDictionary(c => c.Command, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 메시지에서 명령어를 추출하여 해당 핸들러를 실행.
    /// 명령어가 없거나 등록되지 않은 경우 false 반환.
    /// </summary>
    public async Task<bool> TryDispatchAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var cmd = message.Text?.Split(' ', '@')[0].ToLowerInvariant();

        if (cmd is null || !_commands.TryGetValue(cmd, out var handler))
            return false;

        await handler.HandleAsync(bot, message, ct);
        return true;
    }
}
