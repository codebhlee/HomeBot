using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeBot.Extensions;

internal static class LoggingExtensions
{
    internal static IServiceCollection AddHomeBotLogging(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }
}
