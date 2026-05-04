using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using HomeBot.Commands;
using HomeBot.Configuration;
using HomeBot.Extensions;
using HomeBot.Factories;
using HomeBot.Models;
using HomeBot.Services;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0050, SKEXP0070

// ── 설정 읽기 ─────────────────────────────────────────────────────────────────
AppSettings settings;
try
{
    settings = await AppConfiguration.LoadAsync();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return;
}

// ── IHost 구성 (QueueWorker는 IHost가 자동으로 시작/종료 관리) ────────────────
var host = Host.CreateDefaultBuilder(args)
    .AddHomeBotServices(settings)
    .Build();

var serviceProvider    = host.Services;
var botClient          = serviceProvider.GetRequiredService<ITelegramBotClient>();
var taskQueue          = serviceProvider.GetRequiredService<ITaskQueue>();
var commandDispatcher  = serviceProvider.GetRequiredService<CommandDispatcher>();
var contextFactory     = serviceProvider.GetRequiredService<AIContextFactory>();
var logger             = serviceProvider.GetRequiredService<ILogger<Program>>();

// ── IHost 시작 (QueueWorker 포함 모든 BackgroundService 자동 시작) ────────────
await host.StartAsync();

logger.LogInformation("--- RTX 4070 AI 서버 가동 시작 ---");

// ── Telegram 수신 시작 ────────────────────────────────────────────────────────
botClient.StartReceiving(
    updateHandler: async (bot, update, ct) =>
    {
        if (update.Message is not { } message) return;

        // ── 명령어 처리 (CommandDispatcher) ──────────────────────────────────
        if (await commandDispatcher.TryDispatchAsync(bot, message, ct)) return;

        // ── AIContext 생성 후 큐에 추가 ───────────────────────────────────────
        var context = await contextFactory.CreateAsync(message, ct);
        if (context is not null)
            await taskQueue.EnqueueAsync(context, ct);
    },
    errorHandler: (_, ex, _, _) =>
    {
        logger.LogError(ex, "Telegram 폴링 오류");
        return Task.CompletedTask;
    },
    receiverOptions: new ReceiverOptions { AllowedUpdates = [] }
);

// ── 종료 대기 ─────────────────────────────────────────────────────────────────
await host.WaitForShutdownAsync();
