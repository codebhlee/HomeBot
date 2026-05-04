using System.Text;
using System.Text.Json.Serialization;

namespace HomeBot.Models;

internal sealed class SdRequest
{
    [JsonPropertyName("prompt")]          public string Prompt         { get; set; } = "";
    [JsonPropertyName("negative_prompt")] public string NegativePrompt { get; set; } = "nsfw, blurry, low quality";
    [JsonPropertyName("steps")]           public int    Steps          { get; set; } = 20;
    [JsonPropertyName("width")]           public int    Width          { get; set; } = 512;
    [JsonPropertyName("height")]          public int    Height         { get; set; } = 512;
    [JsonPropertyName("cfg_scale")]       public double CfgScale       { get; set; } = 7.0;

    // 예: "a sunset --size 768x512 --steps 30 --cfg 9 --neg ugly"
    public static SdRequest Parse(string input)
    {
        var req = new SdRequest();
        var promptBuilder = new StringBuilder();
        var tokens = input.Split(' ');

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
                        req.Width  = Clamp(w, 64, 2048);
                        req.Height = Clamp(h, 64, 2048);
                    }
                    break;

                case "--steps" when i + 1 < tokens.Length:
                    if (int.TryParse(tokens[++i], out var steps))
                        req.Steps = Clamp(steps, 1, 150);
                    break;

                case "--cfg" when i + 1 < tokens.Length:
                    if (double.TryParse(tokens[++i], out var cfg))
                        req.CfgScale = cfg;
                    break;

                case "--neg" when i + 1 < tokens.Length:
                    req.NegativePrompt = string.Join(' ', tokens[(i + 1)..]);
                    i = tokens.Length;
                    break;

                default:
                    if (!tokens[i].StartsWith("--"))
                        promptBuilder.Append(tokens[i]).Append(' ');
                    break;
            }
        }

        req.Prompt = promptBuilder.ToString().Trim();
        return req;
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
}

internal sealed class SdResponse
{
    [JsonPropertyName("images")] public string[]? Images { get; set; }
}
