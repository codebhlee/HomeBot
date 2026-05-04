using System.Text;
using System.Text.Json;
using HomeBot.Models;
using Microsoft.Extensions.Logging;

namespace HomeBot.Services;

internal sealed class StableDiffusionClient
{
    private readonly IHttpClientFactory    _factory;
    private readonly string[]              _endpoints;
    private readonly ILogger<StableDiffusionClient> _logger;
    private int _index = -1;

    public StableDiffusionClient(IHttpClientFactory factory, string[] endpoints, ILogger<StableDiffusionClient> logger)
    {
        _factory   = factory;
        _endpoints = endpoints;
        _logger    = logger;
    }

    private string NextEndpoint()
    {
        if (_endpoints.Length == 0)
            throw new InvalidOperationException("Stable Diffusion 엔드포인트가 설정되지 않았습니다.");
        var i = (int)((uint)Interlocked.Increment(ref _index) % (uint)_endpoints.Length);
        return _endpoints[i];
    }

    public async Task<byte[]> GenerateAsync(string prompt, CancellationToken ct)
        => await GenerateAsync(SdRequest.Parse(prompt), ct);

    public async Task<byte[]> GenerateAsync(SdRequest request, CancellationToken ct)
    {
        var endpoint = NextEndpoint();
        _logger.LogInformation("SD 요청 | 엔드포인트:{Endpoint}", endpoint);

        var body     = JsonSerializer.Serialize(request, OllamaJsonContext.Default.SdRequest);
        var content  = new StringContent(body, Encoding.UTF8, "application/json");

        var http     = _factory.CreateClient("sd");
        var response = await http.PostAsync($"{endpoint}/sdapi/v1/txt2img", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBytes = await response.Content.ReadAsByteArrayAsync(ct);
        var sdResponse    = JsonSerializer.Deserialize(responseBytes, OllamaJsonContext.Default.SdResponse);

        if (sdResponse?.Images is not { Length: > 0 })
            throw new InvalidOperationException("SD 응답에 이미지가 없습니다.");

        return Convert.FromBase64String(sdResponse.Images[0]);
    }
}
