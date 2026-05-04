using HomeBot.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace HomeBot.Services;

/// <summary>
/// 큐 소비 워커 — BackgroundService로 실행
/// ITaskQueue에서 AIContext를 꺼내 ChatService / ImageService로 라우팅.
/// 동시성: SemaphoreSlim(3)으로 최대 3개 작업 병렬 처리.
/// Graceful Shutdown: stoppingToken 발생 시 새 항목 추가를 막고
/// 진행 중인 작업까지 모두 완료한 뒤 종료.
/// </summary>
public sealed class QueueWorker : BackgroundService
{
    private const int MaxConcurrency = 3;

    private readonly ITaskQueue         _queue;
    private readonly IChatService       _chatService;
    private readonly IImageService      _imageService;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<QueueWorker> _logger;
    private readonly SemaphoreSlim      _semaphore = new(MaxConcurrency, MaxConcurrency);

    public QueueWorker(
        ITaskQueue            queue,
        IChatService          chatService,
        IImageService         imageService,
        ITelegramBotClient    bot,
        ILogger<QueueWorker>  logger)
    {
        _queue        = queue;
        _chatService  = chatService;
        _imageService = imageService;
        _bot          = bot;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueWorker 시작됨 (최대 동시 처리: {MaxConcurrency})", MaxConcurrency);

        stoppingToken.Register(() =>
        {
            _logger.LogInformation("QueueWorker 종료 신호 수신 — 남은 작업 {Count}개 처리 후 종료", _queue.Count);
            _queue.CompleteAdding();
        });

        var activeTasks = new List<Task>();

        await foreach (var context in _queue.ReadAllAsync())
        {
            await _semaphore.WaitAsync();

            var task = Task.Run(async () =>
            {
                try   { await ProcessAsync(context); }
                finally { _semaphore.Release(); }
            });

            activeTasks.Add(task);
            activeTasks.RemoveAll(t => t.IsCompleted);
        }

        if (activeTasks.Count > 0)
        {
            _logger.LogInformation("QueueWorker 진행 중인 작업 {Count}개 완료 대기 중...", activeTasks.Count);
            await Task.WhenAll(activeTasks);
        }

        _logger.LogInformation("QueueWorker 모든 작업 완료 — 종료");
    }

    private async Task ProcessAsync(AIContext context)
    {
        try
        {
            switch (context.TaskType)
            {
                case AITaskType.Chat:
                    await _chatService.RespondAsync(
                        _bot,
                        context.ChatId,
                        context.ModelName,
                        context.Prompt,
                        context.Temperature,
                        context.NumPredict,
                        context.CustomSystemPrompt,
                        CancellationToken.None);
                    break;

                case AITaskType.Image:
                    await _imageService.GenerateAndSendAsync(
                        _bot,
                        context.ChatId,
                        context.Prompt,
                        CancellationToken.None);
                    break;

                default:
                    _logger.LogWarning("알 수 없는 작업 타입: {TaskType} | chatId:{ChatId}", context.TaskType, context.ChatId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "작업 처리 오류 | chatId:{ChatId}", context.ChatId);
            try
            {
                await _bot.SendMessage(context.ChatId, $"처리 중 오류가 발생했습니다: {ex.Message}");
            }
            catch { /* 알림 실패는 무시 */ }
        }
    }
}
