using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace EnviroWatch.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public WeatherService(AppDbContext context,
            IConfiguration config, HttpClient httpClient)
        {
            _context = context;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<WeatherSnapshot?> GetCurrentWeatherAsync(
            int districtId)
        {
            var district = await _context.Districts
                .FindAsync(districtId);
            if (district == null) return null;

            var apiKey = _config["ApiKeys:OpenWeatherMap"];
            var url = $"https://api.openweathermap.org/data/2.5/weather?" +
                $"lat={district.Latitude}&lon={district.Longitude}" +
                $"&appid={apiKey}&units=metric";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var snapshot = new WeatherSnapshot
                {
                    DistrictId = districtId,
                    Temperature = json["main"]?["temp"]?.Value<double>() ?? 0,
                    Humidity = json["main"]?["humidity"]?.Value<double>() ?? 0,
                    WindSpeed = json["wind"]?["speed"]?.Value<double>() ?? 0,
                    Pressure = json["main"]?["pressure"]?.Value<double>() ?? 0,
                    WeatherCondition = json["weather"]?[0]?["main"]
                        ?.Value<string>(),
                    WeatherIcon = json["weather"]?[0]?["icon"]?.Value<string>(),
                    Timestamp = DateTime.UtcNow
                };
                return snapshot;
            }
            catch { return null; }
        }

        public async Task<List<WeatherSnapshot>> GetHistoricalWeatherAsync(
            int districtId, int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            return await _context.WeatherSnapshots
                .Where(w => w.DistrictId == districtId && w.Timestamp >= cutoff)
                .OrderBy(w => w.Timestamp)
                .ToListAsync();
        }
    }
}
