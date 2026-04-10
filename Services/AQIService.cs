using System.Text.Json;
using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Services
{
    public class AQIService : IAQIService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<AQIService> _logger;

        public AQIService(HttpClient http, IConfiguration config, AppDbContext db, ILogger<AQIService> logger)
        {
            _http = http;
            _config = config;
            _db = db;
            _logger = logger;
        }

        public async Task<AQISnapshot?> GetCurrentAQIAsync(District district)
        {
            try
            {
                var apiKey = _config["OpenWeatherMap:ApiKey"];
                var url = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={district.Latitude}&lon={district.Longitude}&appid={apiKey}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AQI API error for {City}: {Status}", district.Name, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);
                var list = data.RootElement.GetProperty("list")[0];
                var components = list.GetProperty("components");

                double pm25 = components.GetProperty("pm2_5").GetDouble();
                double pm10 = components.GetProperty("pm10").GetDouble();
                double o3 = components.GetProperty("o3").GetDouble();
                double no2 = components.GetProperty("no2").GetDouble();
                double so2 = components.GetProperty("so2").GetDouble();
                double co = components.GetProperty("co").GetDouble();

                // Calculate Indian NAQI (National Air Quality Index) using CPCB breakpoints
                var subIndices = new Dictionary<string, int>
                {
                    { "PM2.5", CalculatePM25SubIndex(pm25) },
                    { "PM10",  CalculatePM10SubIndex(pm10) },
                    { "O3",    CalculateO3SubIndex(o3) },
                    { "NO2",   CalculateNO2SubIndex(no2) },
                    { "SO2",   CalculateSO2SubIndex(so2) },
                    { "CO",    CalculateCOSubIndex(co / 1000.0) }  // API gives µg/m³, formula uses mg/m³
                };

                var dominantEntry = subIndices.OrderByDescending(x => x.Value).First();
                int aqi = dominantEntry.Value;
                string dominantPollutant = dominantEntry.Key;

                var snapshot = new AQISnapshot
                {
                    DistrictId = district.Id,
                    AQI = aqi,
                    Category = GetAQICategory(aqi),
                    PM25 = pm25,
                    PM10 = pm10,
                    O3 = o3,
                    NO2 = no2,
                    SO2 = so2,
                    CO = co,
                    DominantPollutant = dominantPollutant,
                    RecordedAt = DateTime.UtcNow
                };

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch AQI for {City}", district.Name);
                return null;
            }
        }

        public async Task<List<AQISnapshot>> GetHistoricalAsync(int districtId, int days)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            return await _db.AQISnapshots
                .Where(a => a.DistrictId == districtId && a.RecordedAt >= since)
                .OrderBy(a => a.RecordedAt)
                .ToListAsync();
        }

        public async Task<List<AQISnapshot>> GetHistoricalFromApiAsync(District district, DateTime start, DateTime end)
        {
            try
            {
                var apiKey = _config["OpenWeatherMap:ApiKey"];
                long startUnix = new DateTimeOffset(start, TimeSpan.Zero).ToUnixTimeSeconds();
                long endUnix = new DateTimeOffset(end, TimeSpan.Zero).ToUnixTimeSeconds();

                var url = $"https://api.openweathermap.org/data/2.5/air_pollution/history?lat={district.Latitude}&lon={district.Longitude}&start={startUnix}&end={endUnix}&appid={apiKey}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AQI History API error for {City}: {Status}", district.Name, response.StatusCode);
                    return new List<AQISnapshot>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);
                var list = data.RootElement.GetProperty("list");

                var snapshots = new List<AQISnapshot>();
                foreach (var item in list.EnumerateArray())
                {
                    var dt = item.GetProperty("dt").GetInt64();
                    var components = item.GetProperty("components");

                    double pm25 = components.GetProperty("pm2_5").GetDouble();
                    double pm10 = components.GetProperty("pm10").GetDouble();
                    double o3 = components.GetProperty("o3").GetDouble();
                    double no2 = components.GetProperty("no2").GetDouble();
                    double so2 = components.GetProperty("so2").GetDouble();
                    double co = components.GetProperty("co").GetDouble();

                    var subIndices = new Dictionary<string, int>
                    {
                        { "PM2.5", CalculatePM25SubIndex(pm25) },
                        { "PM10",  CalculatePM10SubIndex(pm10) },
                        { "O3",    CalculateO3SubIndex(o3) },
                        { "NO2",   CalculateNO2SubIndex(no2) },
                        { "SO2",   CalculateSO2SubIndex(so2) },
                        { "CO",    CalculateCOSubIndex(co / 1000.0) }
                    };

                    var dominantEntry = subIndices.OrderByDescending(x => x.Value).First();
                    int aqi = dominantEntry.Value;

                    snapshots.Add(new AQISnapshot
                    {
                        DistrictId = district.Id,
                        AQI = aqi,
                        Category = GetAQICategory(aqi),
                        PM25 = pm25,
                        PM10 = pm10,
                        O3 = o3,
                        NO2 = no2,
                        SO2 = so2,
                        CO = co,
                        DominantPollutant = dominantEntry.Key,
                        RecordedAt = DateTimeOffset.FromUnixTimeSeconds(dt).UtcDateTime
                    });
                }

                return snapshots.OrderBy(s => s.RecordedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch AQI history for {City}", district.Name);
                return new List<AQISnapshot>();
            }
        }

        // ============================================================
        // Indian NAQI Sub-Index Calculations (CPCB Breakpoint Tables)
        // ============================================================

        private static string GetAQICategory(int aqi) => aqi switch
        {
            <= 50  => "Good",
            <= 100 => "Satisfactory",
            <= 200 => "Moderate",
            <= 300 => "Poor",
            <= 400 => "Very Poor",
            _      => "Severe"
        };

        private static int CalculateSubIndex(double concentration, double[] bpLow, double[] bpHigh, int[] aqiLow, int[] aqiHigh)
        {
            for (int i = 0; i < bpLow.Length; i++)
            {
                if (concentration >= bpLow[i] && concentration <= bpHigh[i])
                {
                    double aqi = ((double)(aqiHigh[i] - aqiLow[i]) / (bpHigh[i] - bpLow[i])) * (concentration - bpLow[i]) + aqiLow[i];
                    return (int)Math.Round(aqi);
                }
            }
            return concentration > bpHigh[^1] ? 500 : 0;
        }

        // PM2.5 breakpoints (µg/m³, 24-hour average) — CPCB India
        private static int CalculatePM25SubIndex(double c) => CalculateSubIndex(c,
            new[] { 0.0,  31.0,  61.0,  91.0,  121.0, 251.0 },
            new[] { 30.0, 60.0,  90.0,  120.0, 250.0, 500.0 },
            new[] { 0,    51,    101,   201,   301,   401 },
            new[] { 50,   100,   200,   300,   400,   500 });

        // PM10 breakpoints (µg/m³, 24-hour average)
        private static int CalculatePM10SubIndex(double c) => CalculateSubIndex(c,
            new[] { 0.0,   51.0,  101.0, 251.0, 351.0, 431.0 },
            new[] { 50.0,  100.0, 250.0, 350.0, 430.0, 500.0 },
            new[] { 0,     51,    101,   201,   301,   401 },
            new[] { 50,    100,   200,   300,   400,   500 });

        // O3 breakpoints (µg/m³, 8-hour average)
        private static int CalculateO3SubIndex(double c) => CalculateSubIndex(c,
            new[] { 0.0,   51.0,  101.0, 169.0, 209.0, 749.0 },
            new[] { 50.0,  100.0, 168.0, 208.0, 748.0, 1000.0 },
            new[] { 0,     51,    101,   201,   301,   401 },
            new[] { 50,    100,   200,   300,   400,   500 });

        // NO2 breakpoints (µg/m³, 24-hour average)
        private static int CalculateNO2SubIndex(double c) => CalculateSubIndex(c,
            new[] { 0.0,   41.0,  81.0,  181.0, 281.0, 401.0 },
            new[] { 40.0,  80.0,  180.0, 280.0, 400.0, 500.0 },
            new[] { 0,     51,    101,   201,   301,   401 },
            new[] { 50,    100,   200,   300,   400,   500 });

        // SO2 breakpoints (µg/m³, 24-hour average)
        private static int CalculateSO2SubIndex(double c) => CalculateSubIndex(c,
            new[] { 0.0,   41.0,  81.0,  381.0, 801.0, 1601.0 },
            new[] { 40.0,  80.0,  380.0, 800.0, 1600.0, 2000.0 },
            new[] { 0,     51,    101,   201,   301,   401 },
            new[] { 50,    100,   200,   300,   400,   500 });

        // CO breakpoints (mg/m³, 8-hour average)
        private static int CalculateCOSubIndex(double c) => CalculateSubIndex(c,
            new[] { 0.0,  1.1, 2.1,  10.1, 17.1, 35.0 },
            new[] { 1.0,  2.0, 10.0, 17.0, 34.0, 50.0 },
            new[] { 0,    51,  101,  201,  301,  401 },
            new[] { 50,   100, 200,  300,  400,  500 });
    }
}
