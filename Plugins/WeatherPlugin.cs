using System.ComponentModel;
using System.Text;
using HomeBot.Models.External;
using HomeBot.Services.Weather;
using Microsoft.SemanticKernel;

namespace HomeBot.Plugins;

/// <summary>
/// SemanticKernel Function Calling용 날씨 플러그인.
/// Kernel에 등록하면 모델이 날씨 관련 질문 시 자동으로 호출.
/// </summary>
public sealed class WeatherPlugin
{
    private readonly IWeatherService _weatherService;

    public WeatherPlugin(IWeatherService weatherService)
        => _weatherService = weatherService;

    [KernelFunction("get_current_weather")]
    [Description("지정한 도시의 현재 날씨(기온, 체감온도, 습도, 풍속, 날씨 설명)를 조회합니다.")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("날씨를 조회할 도시명 (예: Seoul, 서울, Tokyo)")] string city,
        CancellationToken cancellationToken = default)
    {
        var w = await _weatherService.GetCurrentAsync(city, cancellationToken);

        return $"""
            📍 {w.CityName}, {w.Country}
            🌡️ 기온: {w.TempCelsius:F1}°C (체감 {w.FeelsLikeCelsius:F1}°C)
            💧 습도: {w.Humidity}%
            💨 풍속: {w.WindSpeedMps:F1} m/s
            ☁️ 날씨: {w.Description}
            """;
    }

    [KernelFunction("get_weather_forecast")]
    [Description("지정한 도시의 5일 날씨 예보(일별 최저/최고 기온, 날씨 설명)를 조회합니다.")]
    public async Task<string> GetWeatherForecastAsync(
        [Description("예보를 조회할 도시명 (예: Seoul, 서울, Tokyo)")] string city,
        CancellationToken cancellationToken = default)
    {
        var forecasts = await _weatherService.GetForecastAsync(city, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"📅 {city} 5일 예보");

        foreach (var f in forecasts)
        {
            sb.AppendLine(
                $"{f.DateTime:MM/dd(ddd)} " +
                $"🌡️ {f.TempMin:F0}°C ~ {f.TempMax:F0}°C " +
                $"☁️ {f.Description}");
        }

        return sb.ToString().TrimEnd();
    }
}
