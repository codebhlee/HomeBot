namespace HomeBot.Models;

/// <summary>
/// user_sessions 테이블 행 매핑
/// </summary>
public sealed class Session
{
    public long             ChatId           { get; init; }
    public SessionSettings  SessionSettings  { get; set; } = new();
    public DateTimeOffset   CreatedAt        { get; init; }
    public DateTimeOffset   LastActiveAt     { get; set; }
}
