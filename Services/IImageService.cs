using Telegram.Bot;

namespace HomeBot.Services;

public interface IImageService
{
    /// <summary>
    /// 사용자별 기본 파라미터를 적용하여 이미지를 생성하고 Telegram으로 전송
    /// </summary>
    Task GenerateAndSendAsync(
        ITelegramBotClient bot,
        long chatId,
        string prompt,
        CancellationToken ct);

    /// <summary>
    /// 사용자별 기본 이미지 파라미터 업데이트
    /// 예) --size 1024x768 --steps 30 --neg ugly
    /// </summary>
    void UpdateUserDefaults(long chatId, string optionString);

    /// <summary>
    /// 사용자의 현재 기본 파라미터 요약 반환
    /// </summary>
    string GetUserDefaultsSummary(long chatId);
}
