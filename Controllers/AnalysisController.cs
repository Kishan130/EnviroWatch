using EnviroWatch.Models;
using EnviroWatch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWeatherService _weatherService;
        private readonly IAQIService _aqiService;
        private readonly IRecommendationService _recommendationService;

        public AnalysisController(AppDbContext db, IWeatherService weatherService, IAQIService aqiService, IRecommendationService recommendationService)
        {
            _db = db;
            _weatherService = weatherService;
            _aqiService = aqiService;
            _recommendationService = recommendationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /api/analysis/india-overview
        [HttpGet("/api/analysis/india-overview")]
        public async Task<IActionResult> GetIndiaOverview()
        {
            var districts = await _db.Districts.ToListAsync();
            var metroDistricts = districts.Where(d => d.IsMetroCity).ToList();

            // Get latest AQI (live from API) for each metro city
            var metroAqiData = new List<object>();
            foreach (var d in metroDistricts)
            {
                var latestAqi = await _aqiService.GetCurrentAQIAsync(d);

                var latestWeather = await _db.WeatherSnapshots
                    .Where(w => w.DistrictId == d.Id)
                    .OrderByDescending(w => w.RecordedAt)
                    .FirstOrDefaultAsync();

                metroAqiData.Add(new
                {
                    d.Id,
                    d.Name,
                    d.State,
                    AQI = latestAqi?.AQI ?? 0,
                    Category = latestAqi?.Category ?? "N/A",
                    PM25 = latestAqi?.PM25 ?? 0,
                    PM10 = latestAqi?.PM10 ?? 0,
                    DominantPollutant = latestAqi?.DominantPollutant ?? "N/A",
                    Temperature = latestWeather?.Temperature ?? 0,
                    Humidity = latestWeather?.Humidity ?? 0,
                    WeatherCondition = latestWeather?.WeatherCondition ?? "N/A"
                });
            }

            // National averages from metro AQI data collected above
            var latestWeatherSnapshots = await _db.WeatherSnapshots
                .GroupBy(w => w.DistrictId)
                .Select(g => g.OrderByDescending(w => w.RecordedAt).FirstOrDefault())
                .ToListAsync();

            // Top polluted cities (from metro data collected above)
            var topPolluted = metroAqiData
                .OrderByDescending(x => ((dynamic)x).AQI)
                .Take(10)
                .ToList();

            // 7-day national average AQI trend
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var dailyAvgAqi = await _db.AQISnapshots
                .Where(a => a.RecordedAt >= sevenDaysAgo)
                .GroupBy(a => a.RecordedAt.Date)
                .Select(g => new { Date = g.Key, AvgAQI = g.Average(a => a.AQI) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fallback: if DB has no AQI snapshots, fetch 7-day history from API for metros
            if (dailyAvgAqi.Count == 0 && metroDistricts.Count > 0)
            {
                var allHistorySnapshots = new List<AQISnapshot>();
                // Fetch for up to 5 metro cities to keep it fast
                foreach (var metro in metroDistricts.Take(5))
                {
                    var history = await _aqiService.GetHistoricalFromApiAsync(metro, sevenDaysAgo, DateTime.UtcNow);
                    allHistorySnapshots.AddRange(history);
                }

                if (allHistorySnapshots.Count > 0)
                {
                    dailyAvgAqi = allHistorySnapshots
                        .GroupBy(a => a.RecordedAt.Date)
                        .Select(g => new { Date = g.Key, AvgAQI = g.Average(a => (double)a.AQI) })
                        .OrderBy(x => x.Date)
                        .ToList();
                }
            }

            var dailyAvgTemp = await _db.WeatherSnapshots
                .Where(w => w.RecordedAt >= sevenDaysAgo)
                .GroupBy(w => w.RecordedAt.Date)
                .Select(g => new { Date = g.Key, AvgTemp = g.Average(w => w.Temperature) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var metroAqiValues = metroAqiData.Select(x => (int)((dynamic)x).AQI).Where(a => a > 0).ToList();

            return Json(new
            {
                nationalAvgAQI = metroAqiValues.Count > 0 ? metroAqiValues.Average() : 0,
                nationalAvgTemp = latestWeatherSnapshots.Where(w => w != null).Select(w => w!.Temperature).DefaultIfEmpty(0).Average(),
                nationalAvgHumidity = latestWeatherSnapshots.Where(w => w != null).Select(w => (double)w!.Humidity).DefaultIfEmpty(0).Average(),
                metroData = metroAqiData,
                topPolluted,
                dailyAvgAqi = dailyAvgAqi.Select(x => new { Date = x.Date.ToString("MMM dd"), x.AvgAQI }),
                dailyAvgTemp = dailyAvgTemp.Select(x => new { Date = x.Date.ToString("MMM dd"), x.AvgTemp }),
                totalCities = districts.Count,
                lastUpdated = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
            });
        }

        // GET: /api/analysis/city/{id}?days=7
        [HttpGet("/api/analysis/city/{id}")]
        public async Task<IActionResult> GetCityAnalysis(int id, int days = 7)
        {
            var district = await _db.Districts.FindAsync(id);
            if (district == null) return NotFound();

            // Fetch AQI history from OpenWeatherMap Air Pollution History API (free!)
            var start = DateTime.UtcNow.AddDays(-days);
            var end = DateTime.UtcNow;
            var aqiHistory = await _aqiService.GetHistoricalFromApiAsync(district, start, end);

            var weatherHistory = await _db.WeatherSnapshots
                .Where(w => w.DistrictId == id && w.RecordedAt >= start)
                .OrderBy(w => w.RecordedAt)
                .ToListAsync();

            // Category distribution
            var categoryDistribution = aqiHistory
                .GroupBy(a => a.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToList();

            // Pollutant averages
            var pollutantAvgs = aqiHistory.Count > 0 ? new
            {
                PM25 = aqiHistory.Average(a => a.PM25),
                PM10 = aqiHistory.Average(a => a.PM10),
                O3 = aqiHistory.Average(a => a.O3),
                NO2 = aqiHistory.Average(a => a.NO2),
                SO2 = aqiHistory.Average(a => a.SO2),
                CO = aqiHistory.Average(a => a.CO)
            } : null;

            // Current stats
            var latestAqi = aqiHistory.LastOrDefault();
            var latestWeather = weatherHistory.LastOrDefault();

            // Fix 6: If no weather history, fetch current weather from API
            object? currentWeatherData = null;
            if (weatherHistory.Count == 0)
            {
                var liveWeather = await _weatherService.GetCurrentWeatherAsync(district);
                if (liveWeather != null)
                {
                    currentWeatherData = new
                    {
                        liveWeather.Temperature,
                        liveWeather.FeelsLike,
                        liveWeather.Humidity,
                        liveWeather.Pressure,
                        liveWeather.WindSpeed,
                        liveWeather.Visibility,
                        liveWeather.CloudCover,
                        liveWeather.WeatherCondition,
                        liveWeather.WeatherIcon,
                        liveWeather.Description
                    };
                }
            }

            return Json(new
            {
                district = new { district.Id, district.Name, district.State },
                current = new
                {
                    aqi = latestAqi != null ? new { latestAqi.AQI, latestAqi.Category, latestAqi.PM25, latestAqi.PM10, latestAqi.DominantPollutant } : null,
                    weather = latestWeather != null ? new { latestWeather.Temperature, latestWeather.Humidity, latestWeather.WindSpeed, latestWeather.WeatherCondition } : null,
                },
                aqiTrend = aqiHistory.Select(a => new { Date = a.RecordedAt.ToString("MMM dd HH:mm"), a.AQI, a.Category }),
                weatherTrend = weatherHistory.Select(w => new
                {
                    Date = w.RecordedAt.ToString("MMM dd HH:mm"),
                    w.Temperature,
                    w.Humidity,
                    w.WindSpeed
                }),
                currentWeather = currentWeatherData,
                pollutantTrend = aqiHistory.Select(a => new { Date = a.RecordedAt.ToString("MMM dd HH:mm"), a.PM25, a.PM10, a.O3, a.NO2, a.SO2, a.CO }),
                categoryDistribution,
                pollutantAvgs,
                stats = new
                {
                    avgAQI = aqiHistory.Count > 0 ? aqiHistory.Average(a => a.AQI) : 0,
                    maxAQI = aqiHistory.Count > 0 ? aqiHistory.Max(a => a.AQI) : 0,
                    minAQI = aqiHistory.Count > 0 ? aqiHistory.Min(a => a.AQI) : 0,
                    avgTemp = weatherHistory.Count > 0 ? weatherHistory.Average(w => w.Temperature) : 0,
                    maxTemp = weatherHistory.Count > 0 ? weatherHistory.Max(w => w.Temperature) : 0,
                    minTemp = weatherHistory.Count > 0 ? weatherHistory.Min(w => w.Temperature) : 0,
                    dataPoints = aqiHistory.Count
                }
            });
        }

        // GET: /api/analysis/comparison?ids=1,2,3
        [HttpGet("/api/analysis/comparison")]
        public async Task<IActionResult> CompareDistricts([FromQuery] string ids)
        {
            if (string.IsNullOrEmpty(ids)) return BadRequest();

            var idList = ids.Split(',').Select(int.Parse).Take(10).ToList();
            var result = new List<object>();

            foreach (var id in idList)
            {
                var district = await _db.Districts.FindAsync(id);
                if (district == null) continue;

                // Use live AQI from API instead of DB snapshots
                var latestAqi = await _aqiService.GetCurrentAQIAsync(district);

                // Try live weather; fall back to latest DB snapshot
                var latestWeather = await _weatherService.GetCurrentWeatherAsync(district)
                    ?? await _db.WeatherSnapshots
                        .Where(w => w.DistrictId == id)
                        .OrderByDescending(w => w.RecordedAt)
                        .FirstOrDefaultAsync();

                result.Add(new
                {
                    district.Id,
                    district.Name,
                    AQI = latestAqi?.AQI ?? 0,
                    Category = latestAqi?.Category ?? "N/A",
                    PM25 = latestAqi?.PM25 ?? 0,
                    PM10 = latestAqi?.PM10 ?? 0,
                    DominantPollutant = latestAqi?.DominantPollutant ?? "N/A",
                    Temperature = latestWeather?.Temperature ?? 0,
                    Humidity = latestWeather?.Humidity ?? 0,
                    WindSpeed = latestWeather?.WindSpeed ?? 0,
                    WeatherCondition = latestWeather?.WeatherCondition ?? "N/A"
                });
            }

            return Json(result);
        }

        // GET: /api/analysis/top-polluted
        [HttpGet("/api/analysis/top-polluted")]
        public async Task<IActionResult> GetTopPolluted()
        {
            var districts = await _db.Districts.ToListAsync();
            var latestAqis = new List<object>();

            foreach (var d in districts)
            {
                var latest = await _db.AQISnapshots
                    .Where(a => a.DistrictId == d.Id)
                    .OrderByDescending(a => a.RecordedAt)
                    .FirstOrDefaultAsync();

                if (latest != null)
                {
                    latestAqis.Add(new { d.Id, d.Name, d.State, latest.AQI, latest.Category, latest.DominantPollutant });
                }
            }

            return Json(latestAqis.OrderByDescending(x => ((dynamic)x).AQI).Take(10));
        }
    }
}
