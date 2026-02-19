using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace EnviroWatch.Services
{
    public class AQIService : IAQIService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AQIService(AppDbContext context, IConfiguration config,
            HttpClient httpClient)
        {
            _context = context;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<AQISnapshot?> GetCurrentAQIAsync(int districtId)
        {
            var district = await _context.Districts.FindAsync(districtId);
            if (district == null) return null;

            var apiKey = _config["ApiKeys:OpenWeatherMap"];
            var url = $"https://api.openweathermap.org/data/2.5/air_pollution?" +
                $"lat={district.Latitude}&lon={district.Longitude}&appid={apiKey}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                var components = json["list"]?[0]?["components"];

                var snapshot = new AQISnapshot
                {
                    DistrictId = districtId,
                    AQI = json["list"]?[0]?["main"]?["aqi"]?.Value<int>() ?? 1,
                    PM25 = components?["pm2_5"]?.Value<double>(),
                    PM10 = components?["pm10"]?.Value<double>(),
                    NO2 = components?["no2"]?.Value<double>(),
                    SO2 = components?["so2"]?.Value<double>(),
                    CO = components?["co"]?.Value<double>(),
                    O3 = components?["o3"]?.Value<double>(),
                    Timestamp = DateTime.UtcNow
                };
                // Convert OWM AQI (1-5) to Indian scale (0-500)
                snapshot.AQI = ConvertToIndianAQI(snapshot.PM25 ?? 0);
                return snapshot;
            }
            catch { return null; }
        }

        private int ConvertToIndianAQI(double pm25)
        {
            // Simplified conversion: PM2.5 μg/m³ to Indian AQI
            if (pm25 <= 30) return (int)(pm25 * 50 / 30);
            if (pm25 <= 60) return 50 + (int)((pm25 - 30) * 50 / 30);
            if (pm25 <= 90) return 100 + (int)((pm25 - 60) * 100 / 30);
            if (pm25 <= 120) return 200 + (int)((pm25 - 90) * 100 / 30);
            if (pm25 <= 250) return 300 + (int)((pm25 - 120) * 100 / 130);
            return 400 + (int)((pm25 - 250) * 100 / 130);
        }

        public async Task<List<AQISnapshot>> GetHistoricalAQIAsync(
            int districtId, int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            return await _context.AQISnapshots
                .Where(a => a.DistrictId == districtId && a.Timestamp >= cutoff)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
        }

        public string GetAQICategory(int aqi)
        {
            if (aqi <= 50) return "Good";
            if (aqi <= 100) return "Satisfactory";
            if (aqi <= 200) return "Moderate";
            if (aqi <= 300) return "Poor";
            if (aqi <= 400) return "Very Poor";
            return "Severe";
        }

        public string GetAQIColor(int aqi)
        {
            if (aqi <= 50) return "#00E400";       // Green
            if (aqi <= 100) return "#FFFF00";      // Yellow
            if (aqi <= 200) return "#FF7E00";      // Orange
            if (aqi <= 300) return "#FF0000";      // Red
            if (aqi <= 400) return "#8F3F97";      // Purple
            return "#7E0023";                      // Maroon
        }
    }
}
