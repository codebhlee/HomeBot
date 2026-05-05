using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using HomeBot.Models;
using HomeBot.Plugins;
using HomeBot.Services;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0050, SKEXP0070

namespace HomeBot.Extensions;

internal static class AiServicesExtensions
{
    internal static IServiceCollection AddAiServices(
        this IServiceCollection services, AppSettings settings)
    {
        var ollamaEndpoint = new Uri(settings.OllamaBaseUrl!);

        // Ollama 임베딩 (TextMemory용 — 별도 Kernel 인스턴스)
        services.AddSingleton<ITextEmbeddingGenerationService>(_ =>
        {
            var embeddingKernel = Kernel.CreateBuilder()
                .AddOllamaTextEmbeddingGeneration(settings.OllamaEmbeddingModel!, ollamaEndpoint)
                .Build();
            return embeddingKernel.GetRequiredService<ITextEmbeddingGenerationService>();
        });

        // SemanticTextMemory (VolatileStore)
        services.AddSingleton<ISemanticTextMemory>(sp =>
        {
            var embedding = sp.GetRequiredService<ITextEmbeddingGenerationService>();
            return new SemanticTextMemory(new VolatileMemoryStore(), embedding);
        });

        // ConversationMemoryService
        services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();

        // SK Kernel (Transient) — Ollama Chat Completion + WeatherPlugin
        // AddKernel()이 Kernel을 Transient로 등록
        // AddOllamaChatCompletion()이 IChatCompletionService 등록
        // Plugins.AddFromType<T>()가 WeatherPlugin을 DI 기반으로 등록
        services.AddKernel()
                .AddOllamaChatCompletion(
                    modelId:  settings.DefaultChatModel ?? "llama3.1:8b-instruct-q8_0",
                    endpoint: ollamaEndpoint)
                .Plugins.AddFromType<WeatherPlugin>();

        // ChatService — Transient (Kernel이 Transient이므로 동일 수명 유지)
        services.AddTransient<IChatService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            var memory = sp.GetRequiredService<IConversationMemoryService>();
            var logger = sp.GetRequiredService<ILogger<ChatService>>();
            return new ChatService(kernel, memory, logger);
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
