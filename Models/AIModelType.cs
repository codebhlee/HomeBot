namespace HomeBot.Models;

public enum AIModelType
{
    /// <summary>qwen2.5-coder:14b — 코딩 특화 모델</summary>
    QwenCoder,

    /// <summary>llama3.1:8b-instruct-q8_0 — 일반 대화 모델</summary>
    Llama31Instruct,

    /// <summary>Stable Diffusion — 이미지 생성</summary>
    StableDiffusion,
}
