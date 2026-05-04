using HomeBot.Models;

namespace HomeBot.Services;

/// <summary>
/// AI 작업 큐 인터페이스
/// 현재는 InMemory(Channel) 구현체를 사용하며,
/// 추후 RabbitMQ / Kafka 구현체로 교체 가능
/// </summary>
public interface ITaskQueue
{
    /// <summary>작업을 큐에 추가</summary>
    ValueTask EnqueueAsync(AIContext context, CancellationToken ct = default);

    /// <summary>큐에서 작업을 꺼냄 (없으면 대기)</summary>
    ValueTask<AIContext> DequeueAsync(CancellationToken ct = default);

    /// <summary>
    /// 큐의 모든 항목을 비동기 스트림으로 소비.
    /// CompleteAdding() 호출 후 남은 항목을 모두 소비하면 자동 완료.
    /// </summary>
    IAsyncEnumerable<AIContext> ReadAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 새 항목 추가를 중단하고 드레인 신호를 보냄.
    /// 이후 남아있는 항목은 모두 소비된 뒤 ReadAllAsync가 완료됨.
    /// </summary>
    void CompleteAdding();

    /// <summary>현재 큐에 남아있는 항목 수</summary>
    int Count { get; }
}
