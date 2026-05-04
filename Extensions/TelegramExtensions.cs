using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using HomeBot.Commands;
using HomeBot.Models;
using HomeBot.Services;

namespace HomeBot.Extensions;

internal static class TelegramExtensions
{
    internal static IServiceCollection AddTelegramServices(
        this IServiceCollection services, AppSettings settings)
    {
        services.AddSingleton<ITelegramBotClient>(_ =>
            new TelegramBotClient(settings.TelegramToken!));

        // Command Dispatcher (Strategy 패턴)
        services.AddSingleton<IBotCommand, CodeCommand>();
        services.AddSingleton<IBotCommand, GenCommand>();
        services.AddSingleton<IBotCommand, DesignCommand>();
        services.AddSingleton<CommandDispatcher>();

        return services;
    }
}
