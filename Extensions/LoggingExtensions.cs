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
            logging.SetMinimumLevel(LogLevel.Debug);
            // SK 내부 로그는 너무 많으므로 Warning 이상만
            logging.AddFilter("Microsoft.SemanticKernel", LogLevel.Warning);
        });

        return services;
    }
}
