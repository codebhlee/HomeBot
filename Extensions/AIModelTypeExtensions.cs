using HomeBot.Models;

namespace HomeBot.Extensions;

public static class AIModelTypeExtensions
{
    /// <summary>Ollama API에 전달할 모델명 문자열 반환</summary>
    public static string ToModelName(this AIModelType model) => model switch
    {
        AIModelType.QwenCoder       => "qwen2.5-coder:14b",
        AIModelType.Llama31Instruct => "llama3.1:8b-instruct-q8_0",
        AIModelType.StableDiffusion => "stable-diffusion",
        _                           => throw new ArgumentOutOfRangeException(nameof(model), model, null),
    };

    /// <summary>모델별 기본 Temperature</summary>
    public static float DefaultTemperature(this AIModelType model) => model switch
    {
        AIModelType.QwenCoder       => 0.2f,
        AIModelType.Llama31Instruct => 0.5f,
        AIModelType.StableDiffusion => 0.7f,
        _                           => 0.4f,
    };

    /// <summary>모델별 기본 NumPredict</summary>
    public static int DefaultNumPredict(this AIModelType model) => model switch
    {
        AIModelType.QwenCoder       => 2048,
        AIModelType.Llama31Instruct => 1024,
        AIModelType.StableDiffusion => 256,
        _                           => 1024,
    };

    /// <summary>이미지 생성 모델 여부</summary>
    public static bool IsImageModel(this AIModelType model)
        => model == AIModelType.StableDiffusion;
}
