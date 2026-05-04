using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HomeBot.Models;
using HomeBot.Services;

namespace HomeBot.Extensions;

public static class ServiceExtensions
{
    public static IHostBuilder AddHomeBotServices(
        this IHostBuilder builder, AppSettings settings)
    {
        return builder.ConfigureServices((_, services) =>
        {
            services
                .AddHomeBotLogging()
                .AddHomeBotHttpClients(settings)
                .AddAiServices(settings)
                .AddInfrastructure(settings)
                .AddTelegramServices(settings)
                .AddHostedService<QueueWorker>();
        });
    }
}
