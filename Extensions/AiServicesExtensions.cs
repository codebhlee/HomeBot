using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using HomeBot.Models;
using HomeBot.Services;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0050, SKEXP0070

namespace HomeBot.Extensions;

internal static class AiServicesExtensions
{
    internal static IServiceCollection AddAiServices(
        this IServiceCollection services, AppSettings settings)
    {
        var ollamaEndpoint = new Uri(settings.OllamaBaseUrl!);

        // Ollama 임베딩
        services.AddSingleton<ITextEmbeddingGenerationService>(_ =>
        {
            var kernel = Kernel.CreateBuilder()
                .AddOllamaTextEmbeddingGeneration(settings.OllamaEmbeddingModel!, ollamaEndpoint)
                .Build();
            return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        });

        // SemanticTextMemory (VolatileStore)
        services.AddSingleton<ISemanticTextMemory>(sp =>
        {
            var embedding = sp.GetRequiredService<ITextEmbeddingGenerationService>();
            return new SemanticTextMemory(new VolatileMemoryStore(), embedding);
        });

        // ConversationMemoryService
        services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();

        // ChatService
        services.AddSingleton<IChatService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var memory  = sp.GetRequiredService<IConversationMemoryService>();
            var logger  = sp.GetRequiredService<ILogger<ChatService>>();
            return new ChatService(ollamaEndpoint, factory, memory, logger);
        });

        // StableDiffusionClient + ImageService
        services.AddSingleton<StableDiffusionClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger  = sp.GetRequiredService<ILogger<StableDiffusionClient>>();
            return new StableDiffusionClient(factory, settings.StableDiffusionEndpoints ?? [], logger);
        });

        services.AddSingleton<IImageService>(sp =>
        {
            var sdClient = sp.GetRequiredService<StableDiffusionClient>();
            var logger   = sp.GetRequiredService<ILogger<ImageService>>();
            return new ImageService(sdClient, logger);
        });

        return services;
    }
}
