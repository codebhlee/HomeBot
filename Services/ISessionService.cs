using HomeBot.Models;

namespace HomeBot.Services;

public interface ISessionService
{
    /// <summary>세션 조회. 없으면 기본값으로 새로 생성하여 반환</summary>
    Task<Session> GetOrCreateAsync(long chatId, CancellationToken ct = default);

    /// <summary>session_settings 저장 + last_active_at 갱신</summary>
    Task SaveAsync(Session session, CancellationToken ct = default);

    /// <summary>last_active_at만 갱신</summary>
    Task TouchAsync(long chatId, CancellationToken ct = default);
}
