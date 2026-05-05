using HomeBot.Models.External;

namespace HomeBot.Services.Weather;

public interface IWeatherService
{
    /// <summary>도시명으로 현재 날씨 조회</summary>
    Task<CurrentWeather> GetCurrentAsync(string city, CancellationToken ct = default);

    /// <summary>도시명으로 5일 예보 조회 (일별 대표값)</summary>
    Task<IReadOnlyList<DailyForecast>> GetForecastAsync(string city, CancellationToken ct = default);
}
