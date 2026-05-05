using System.Text.Json.Serialization;

namespace HomeBot.Models.External;

// ── OpenWeatherMap API v2.5 응답 매핑 ────────────────────────────────────────

internal sealed class OpenWeatherMapCurrentResponse
{
    [JsonPropertyName("name")]    public string?                    Name    { get; init; }
    [JsonPropertyName("sys")]     public OpenWeatherMapSys?         Sys     { get; init; }
    [JsonPropertyName("main")]    public OpenWeatherMapMain?        Main    { get; init; }
    [JsonPropertyName("wind")]    public OpenWeatherMapWind?        Wind    { get; init; }
    [JsonPropertyName("weather")] public OpenWeatherMapWeather[]?   Weather { get; init; }
}

internal sealed class OpenWeatherMapForecastResponse
{
    [JsonPropertyName("list")] public OpenWeatherMapForecastItem[]? List { get; init; }
    [JsonPropertyName("city")] public OpenWeatherMapCity?           City { get; init; }
}

internal sealed class OpenWeatherMapForecastItem
{
    [JsonPropertyName("dt")]      public long                      Dt      { get; init; }
    [JsonPropertyName("main")]    public OpenWeatherMapMain?       Main    { get; init; }
    [JsonPropertyName("weather")] public OpenWeatherMapWeather[]?  Weather { get; init; }
}

internal sealed class OpenWeatherMapMain
{
    [JsonPropertyName("temp")]       public double Temp      { get; init; }
    [JsonPropertyName("feels_like")] public double FeelsLike { get; init; }
    [JsonPropertyName("temp_min")]   public double TempMin   { get; init; }
    [JsonPropertyName("temp_max")]   public double TempMax   { get; init; }
    [JsonPropertyName("humidity")]   public int    Humidity  { get; init; }
}

internal sealed class OpenWeatherMapWind
{
    [JsonPropertyName("speed")] public double Speed { get; init; }
}

internal sealed class OpenWeatherMapWeather
{
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("icon")]        public string? Icon        { get; init; }
}

internal sealed class OpenWeatherMapSys
{
    [JsonPropertyName("country")] public string? Country { get; init; }
}

internal sealed class OpenWeatherMapCity
{
    [JsonPropertyName("name")]    public string? Name    { get; init; }
    [JsonPropertyName("country")] public string? Country { get; init; }
}
