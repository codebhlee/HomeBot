namespace HomeBot.Models;

/// <summary>
/// 사용자 세션 설정 — sessions.session_settings JSONB 컬럼에 저장
/// </summary>
public sealed class SessionSettings
{
    public AIModelType CurrentMode  { get; set; } = AIModelType.Llama31Instruct;
    public float       Temperature  { get; set; } = 0.5f;
    public int?        MaxTokens    { get; set; }

    /// <summary>추가적인 유연성을 위한 메타데이터 (YAGNI 대비)</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    // 모델별 커스텀 프롬프트 (가장 강력한 페르소나 도구)
    public Dictionary<string, string> CustomSystemPrompts { get; set; } = new();
}
