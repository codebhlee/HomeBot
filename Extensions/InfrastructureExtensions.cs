using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HomeBot.Factories;
using HomeBot.Models;
using HomeBot.Services;
using HomeBot.Services.Weather;

namespace HomeBot.Extensions;

internal static class InfrastructureExtensions
{
    internal static IServiceCollection AddInfrastructure(
        this IServiceCollection services, AppSettings settings)
    {
        // TaskQueue (InMemory Channel, capacity 100)
        services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();

        // SessionService (PostgreSQL + Dapper)
        services.AddSingleton<ISessionService>(_ =>
            new PostgresSessionService(settings.PostgresConnectionString!));

        // WeatherService (OpenWeatherMap) — Singleton (stateless, HttpClient 재사용)
        services.AddSingleton<IWeatherService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger  = sp.GetRequiredService<ILogger<WeatherService>>();
            return new WeatherService(factory, settings.OpenWeatherApiKey!, logger);
        });

        // AIContextFactory
        services.AddSingleton<AIContextFactory>();

        return services;
    }
}
