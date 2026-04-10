using System.Text.Json;
using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient http, IConfiguration config, AppDbContext db, ILogger<WeatherService> logger)
        {
            _http = http;
            _config = config;
            _db = db;
            _logger = logger;
        }

        public async Task<WeatherSnapshot?> GetCurrentWeatherAsync(District district)
        {
            try
            {
                var apiKey = _config["OpenWeatherMap:ApiKey"];
                var url = $"https://api.openweathermap.org/data/2.5/weather?lat={district.Latitude}&lon={district.Longitude}&appid={apiKey}&units=metric";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeather API error for {City}: {Status}", district.Name, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);
                var root = data.RootElement;

                var main = root.GetProperty("main");
                var wind = root.GetProperty("wind");
                var sys = root.GetProperty("sys");
                var weather = root.GetProperty("weather")[0];
                var clouds = root.GetProperty("clouds");

                var snapshot = new WeatherSnapshot
                {
                    DistrictId = district.Id,
                    Temperature = main.GetProperty("temp").GetDouble(),
                    FeelsLike = main.GetProperty("feels_like").GetDouble(),
                    Humidity = main.GetProperty("humidity").GetInt32(),
                    Pressure = main.GetProperty("pressure").GetDouble(),
                    WindSpeed = wind.GetProperty("speed").GetDouble(),
                    WindDirection = wind.TryGetProperty("deg", out var deg) ? deg.GetInt32() : 0,
                    Visibility = root.TryGetProperty("visibility", out var vis) ? vis.GetInt32() : 10000,
                    CloudCover = clouds.GetProperty("all").GetInt32(),
                    WeatherCondition = weather.GetProperty("main").GetString() ?? "Unknown",
                    WeatherIcon = weather.GetProperty("icon").GetString() ?? "01d",
                    Description = weather.GetProperty("description").GetString() ?? "",
                    Sunrise = DateTimeOffset.FromUnixTimeSeconds(sys.GetProperty("sunrise").GetInt64()).UtcDateTime,
                    Sunset = DateTimeOffset.FromUnixTimeSeconds(sys.GetProperty("sunset").GetInt64()).UtcDateTime,
                    RecordedAt = DateTime.UtcNow
                };

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch weather for {City}", district.Name);
                return null;
            }
        }

        public async Task<List<WeatherSnapshot>> GetHistoricalAsync(int districtId, int days)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            return await _db.WeatherSnapshots
                .Where(w => w.DistrictId == districtId && w.RecordedAt >= since)
                .OrderBy(w => w.RecordedAt)
                .ToListAsync();
        }
    }
}
