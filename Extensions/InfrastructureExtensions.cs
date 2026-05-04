using Microsoft.Extensions.DependencyInjection;
using HomeBot.Factories;
using HomeBot.Models;
using HomeBot.Services;

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

        // AIContextFactory
        services.AddSingleton<AIContextFactory>();

        return services;
    }
}
