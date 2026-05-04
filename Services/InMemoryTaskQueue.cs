using System.Threading.Channels;
using HomeBot.Models;

namespace HomeBot.Services;

/// <summary>
/// C# Channel 기반 인메모리 큐 구현체
/// 백프레셔: capacity 초과 시 EnqueueAsync가 대기 (BoundedChannelFullMode.Wait)
/// </summary>
public sealed class InMemoryTaskQueue : ITaskQueue
{
    private readonly Channel<AIContext> _channel;

    public InMemoryTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode              = BoundedChannelFullMode.Wait,  // 백프레셔
            SingleReader          = false,                         // 멀티 워커 확장 가능
            SingleWriter          = false,
            AllowSynchronousContinuations = false,
        };
        _channel = Channel.CreateBounded<AIContext>(options);
    }

    public ValueTask EnqueueAsync(AIContext context, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(context, ct);

    public ValueTask<AIContext> DequeueAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAsync(ct);

    /// <summary>
    /// Channel이 Complete될 때까지 모든 항목을 스트리밍으로 반환.
    /// CompleteAdding() 호출 후 남은 항목을 소비하면 자동 종료.
    /// </summary>
    public IAsyncEnumerable<AIContext> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);

    /// <summary>
    /// 새 항목 추가를 중단. 이후 Reader는 남은 항목을 모두 소비한 뒤 완료됨.
    /// </summary>
    public void CompleteAdding() => _channel.Writer.TryComplete();

    /// <summary>현재 큐에 남아있는 항목 수</summary>
    public int Count => _channel.Reader.Count;
}
