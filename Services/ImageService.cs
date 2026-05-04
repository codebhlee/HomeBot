using System.Collections.Concurrent;
using HomeBot.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HomeBot.Services;

/// <summary>
/// Stable Diffusion 이미지 생성 서비스
/// </summary>
internal sealed class ImageService : IImageService
{
    private readonly StableDiffusionClient      _sdClient;
    private readonly ILogger<ImageService>      _logger;
    private readonly ConcurrentDictionary<long, ImageContext> _userContexts = new();

    public ImageService(StableDiffusionClient sdClient, ILogger<ImageService> logger)
    {
        _sdClient = sdClient;
        _logger   = logger;
    }

    public async Task GenerateAndSendAsync(
        ITelegramBotClient bot,
        long chatId,
        string prompt,
        CancellationToken ct)
    {
        _logger.LogInformation("이미지 생성 시작 | chatId:{ChatId} | {Prompt}", chatId, prompt);

        try
        {
            await bot.SendChatAction(chatId, ChatAction.UploadPhoto, cancellationToken: ct);

            var request = BuildRequest(chatId, prompt);
            _logger.LogInformation("이미지 파라미터 | chatId:{ChatId} | {W}x{H} steps:{Steps} cfg:{Cfg}",
                chatId, request.Width, request.Height, request.Steps, request.CfgScale);

            var imageBytes = await _sdClient.GenerateAsync(request, ct);
            using var stream = new MemoryStream(imageBytes);
            await bot.SendPhoto(chatId, InputFile.FromStream(stream, "image.png"), cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "이미지 생성 오류 | chatId:{ChatId}", chatId);
            await bot.SendMessage(chatId, $"이미지 생성 실패: {ex.Message}", cancellationToken: ct);
        }
    }

    public void UpdateUserDefaults(long chatId, string optionString)
    {
        var ctx = _userContexts.GetOrAdd(chatId, _ => new ImageContext());
        ctx.ApplyOptions(optionString);
    }

    public string GetUserDefaultsSummary(long chatId)
    {
        if (!_userContexts.TryGetValue(chatId, out var ctx))
            return "기본값 사용 중 (512x512, steps:20, cfg:7.0)";

        return $"{ctx.Width}x{ctx.Height} | steps:{ctx.Steps} | cfg:{ctx.CfgScale}" +
               (string.IsNullOrEmpty(ctx.NegativePrompt) ? "" : $" | neg:{ctx.NegativePrompt}");
    }

    // 인라인 프롬프트 옵션을 파싱하고 사용자 기본값과 병합
    private SdRequest BuildRequest(long chatId, string prompt)
    {
        var request = SdRequest.Parse(prompt);

        // 사용자 기본값이 있고 인라인에서 명시하지 않은 항목은 기본값 적용
        if (_userContexts.TryGetValue(chatId, out var ctx))
        {
            // 인라인에서 기본값(512)과 같으면 사용자 설정값 사용
            if (request.Width == 512 && ctx.Width != 512)   request.Width  = ctx.Width;
            if (request.Height == 512 && ctx.Height != 512) request.Height = ctx.Height;
            if (request.Steps == 20 && ctx.Steps != 20)     request.Steps  = ctx.Steps;
            if (request.CfgScale == 7.0 && ctx.CfgScale != 7.0) request.CfgScale = ctx.CfgScale;

            // 네거티브 프롬프트: 인라인에 없으면 기본값 사용
            if (request.NegativePrompt == "nsfw, blurry, low quality" && !string.IsNullOrEmpty(ctx.NegativePrompt))
                request.NegativePrompt = ctx.NegativePrompt;
        }

        return request;
    }
}

/// <summary>
/// 사용자별 이미지 생성 기본 파라미터
/// </summary>
internal sealed class ImageContext
{
    public int    Width          { get; set; } = 512;
    public int    Height         { get; set; } = 512;
    public int    Steps          { get; set; } = 20;
    public double CfgScale       { get; set; } = 7.0;
    public string NegativePrompt { get; set; } = "";

    // /imgset --size 1024x768 --steps 30 --neg ugly 형태로 기본값 업데이트
    public void ApplyOptions(string optionString)
    {
        var tokens = optionString.Split(' ');
        for (int i = 0; i < tokens.Length; i++)
        {
            switch (tokens[i].ToLowerInvariant())
            {
                case "--size" when i + 1 < tokens.Length:
                    var parts = tokens[++i].Split('x');
                    if (parts.Length == 2
                        && int.TryParse(parts[0], out var w)
                        && int.TryParse(parts[1], out var h))
                    {
                        Width  = Math.Clamp(w, 64, 2048);
                        Height = Math.Clamp(h, 64, 2048);
                    }
                    break;
                case "--steps" when i + 1 < tokens.Length:
                    if (int.TryParse(tokens[++i], out var steps))
                        Steps = Math.Clamp(steps, 1, 150);
                    break;
                case "--cfg" when i + 1 < tokens.Length:
                    if (double.TryParse(tokens[++i], out var cfg))
                        CfgScale = cfg;
                    break;
                case "--neg" when i + 1 < tokens.Length:
                    NegativePrompt = string.Join(' ', tokens[(i + 1)..]);
                    i = tokens.Length;
                    break;
            }
        }
    }
}
