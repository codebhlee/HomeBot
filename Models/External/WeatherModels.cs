namespace HomeBot.Models.External;

/// <summary>현재 날씨 조회 결과</summary>
public sealed record CurrentWeather(
    string CityName,
    string Country,
    double TempCelsius,
    double FeelsLikeCelsius,
    int    Humidity,
    double WindSpeedMps,
    string Description,
    string Icon
);

/// <summary>일별 예보 항목</summary>
public sealed record DailyForecast(
    DateTimeOffset DateTime,
    double         TempMin,
    double         TempMax,
    string         Description,
    string         Icon
);
