using Microsoft.Extensions.DependencyInjection;
using HomeBot.Models;

namespace HomeBot.Extensions;

internal static class HttpClientExtensions
{
    internal static IServiceCollection AddHomeBotHttpClients(
        this IServiceCollection services, AppSettings settings)
    {
        services.AddHttpClient("sd", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddHttpClient("ollama", client =>
        {
            client.BaseAddress = new Uri(settings.OllamaBaseUrl!);
            client.Timeout     = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient("weather", client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org");
            client.Timeout     = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
