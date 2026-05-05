using System.Text.Json;
using HomeBot.Models.External;
using Microsoft.Extensions.Logging;

namespace HomeBot.Services.Weather;

/// <summary>
/// OpenWeatherMap API v2.5 기반 날씨 서비스
/// - GET /data/2.5/weather  : 현재 날씨
/// - GET /data/2.5/forecast : 5일 / 3시간 간격 예보 → 일별로 집계
/// </summary>
public sealed class WeatherService : IWeatherService
{
    private const string BaseUrl = "https://api.openweathermap.org";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory      _httpFactory;
    private readonly string                  _apiKey;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(IHttpClientFactory httpFactory, string apiKey, ILogger<WeatherService> logger)
    {
        _httpFactory = httpFactory;
        _apiKey      = apiKey;
        _logger      = logger;
    }

    public async Task<CurrentWeather> GetCurrentAsync(string city, CancellationToken ct = default)
    {
        _logger.LogInformation("현재 날씨 조회 | city:{City}", city);

        var url      = $"/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=kr";
        var http     = _httpFactory.CreateClient("weather");
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsByteArrayAsync(ct);
        var owm  = JsonSerializer.Deserialize<OpenWeatherMapCurrentResponse>(body, JsonOptions)
                   ?? throw new InvalidOperationException("날씨 응답 파싱 실패");

        return new CurrentWeather(
            CityName:         owm.Name           ?? city,
            Country:          owm.Sys?.Country   ?? string.Empty,
            TempCelsius:      owm.Main?.Temp      ?? 0,
            FeelsLikeCelsius: owm.Main?.FeelsLike ?? 0,
            Humidity:         owm.Main?.Humidity  ?? 0,
            WindSpeedMps:     owm.Wind?.Speed     ?? 0,
            Description:      owm.Weather?.FirstOrDefault()?.Description ?? string.Empty,
            Icon:             owm.Weather?.FirstOrDefault()?.Icon        ?? string.Empty
        );
    }

    public async Task<IReadOnlyList<DailyForecast>> GetForecastAsync(string city, CancellationToken ct = default)
    {
        _logger.LogInformation("5일 예보 조회 | city:{City}", city);

        var url      = $"/data/2.5/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=kr";
        var http     = _httpFactory.CreateClient("weather");
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsByteArrayAsync(ct);
        var owm  = JsonSerializer.Deserialize<OpenWeatherMapForecastResponse>(body, JsonOptions)
                   ?? throw new InvalidOperationException("예보 응답 파싱 실패");

        // 3시간 간격 데이터를 날짜별로 그룹핑 → 일별 min/max 집계
        var daily = (owm.List ?? [])
            .GroupBy(item => DateTimeOffset.FromUnixTimeSeconds(item.Dt).ToLocalTime().Date)
            .Select(g =>
            {
                var rep = g.OrderBy(x => Math.Abs(
                    DateTimeOffset.FromUnixTimeSeconds(x.Dt).ToLocalTime().Hour - 12)).First();

                return new DailyForecast(
                    DateTime:    DateTimeOffset.FromUnixTimeSeconds(g.First().Dt).ToLocalTime(),
                    TempMin:     g.Min(x => x.Main?.TempMin ?? 0),
                    TempMax:     g.Max(x => x.Main?.TempMax ?? 0),
                    Description: rep.Weather?.FirstOrDefault()?.Description ?? string.Empty,
                    Icon:        rep.Weather?.FirstOrDefault()?.Icon        ?? string.Empty
                );
            })
            .Take(5)
            .ToList();

        return daily;
    }
}
