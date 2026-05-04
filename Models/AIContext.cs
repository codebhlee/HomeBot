using HomeBot.Extensions;

namespace HomeBot.Models;

/// <summary>
/// 큐에 담길 AI 작업 컨텍스트
/// Telegram 메시지 수신 시 생성되어 큐에 저장됨
/// </summary>
public sealed record AIContext(
    long        ChatId,
    AITaskType  TaskType,
    AIModelType ModelType,
    string      Prompt,
    float       Temperature        = 0.5f,
    int         NumPredict         = 1024,
    string?     CustomSystemPrompt = null
)
{
    /// <summary>Ollama API 전달용 모델명 문자열</summary>
    public string ModelName => ModelType.ToModelName();
}
