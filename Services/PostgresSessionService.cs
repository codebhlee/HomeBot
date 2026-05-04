using System.Text.Json;
using Dapper;
using HomeBot.Models;
using Npgsql;

namespace HomeBot.Services;

/// <summary>
/// Dapper + PostgreSQL 기반 세션 서비스
/// session_settings 컬럼은 JSONB로 저장/조회
/// </summary>
public sealed class PostgresSessionService : ISessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _connectionString;

    public PostgresSessionService(string connectionString)
    {
        _connectionString = connectionString;

        // Npgsql이 JSONB 컬럼을 string으로 읽도록 매핑
        SqlMapper.AddTypeHandler(new SessionSettingsTypeHandler());
    }

    public async Task<Session> GetOrCreateAsync(long chatId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
            INSERT INTO user_sessions (chat_id, session_settings, created_at, last_active_at)
            VALUES (@ChatId, @Settings::jsonb, NOW(), NOW())
            ON CONFLICT (chat_id) DO UPDATE
                SET last_active_at = NOW();

            SELECT chat_id, session_settings, created_at, last_active_at
            FROM user_sessions
            WHERE chat_id = @ChatId;
            """;

        var defaultSettings = JsonSerializer.Serialize(new SessionSettings(), JsonOptions);

        var row = await conn.QuerySingleAsync<SessionRow>(
            new CommandDefinition(sql, new { ChatId = chatId, Settings = defaultSettings },
                cancellationToken: ct));

        return row.ToSession();
    }

    public async Task SaveAsync(Session session, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
            UPDATE user_sessions
            SET session_settings = @Settings::jsonb,
                last_active_at   = NOW()
            WHERE chat_id = @ChatId;
            """;

        var json = JsonSerializer.Serialize(session.SessionSettings, JsonOptions);

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { ChatId = session.ChatId, Settings = json },
                cancellationToken: ct));
    }

    public async Task TouchAsync(long chatId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = """
            UPDATE user_sessions
            SET last_active_at = NOW()
            WHERE chat_id = @ChatId;
            """;

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { ChatId = chatId },
                cancellationToken: ct));
    }

    // ── 내부 매핑 ─────────────────────────────────────────────────────────────

    /// <summary>Dapper raw 행 — JSONB는 string으로 수신 후 역직렬화</summary>
    private sealed class SessionRow
    {
        public long           chat_id          { get; init; }
        public string         session_settings { get; init; } = "{}";
        public DateTimeOffset created_at       { get; init; }
        public DateTimeOffset last_active_at   { get; init; }

        public Session ToSession() => new()
        {
            ChatId          = chat_id,
            SessionSettings = JsonSerializer.Deserialize<SessionSettings>(
                                  session_settings, JsonOptions) ?? new SessionSettings(),
            CreatedAt       = created_at,
            LastActiveAt    = last_active_at,
        };
    }

    /// <summary>Dapper TypeHandler — SessionSettings ↔ JSONB string</summary>
    private sealed class SessionSettingsTypeHandler : SqlMapper.TypeHandler<SessionSettings>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, SessionSettings? value)
            => parameter.Value = JsonSerializer.Serialize(value ?? new SessionSettings(), JsonOptions);

        public override SessionSettings Parse(object value)
            => JsonSerializer.Deserialize<SessionSettings>(value.ToString()!, JsonOptions)
               ?? new SessionSettings();
    }
}
