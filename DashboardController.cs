using EnviroWatch.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/dashboard/districts
        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts()
        {
            var districts = await _db.Districts
                .Where(d => d.IsActive)
                .OrderBy(d => d.StateName)
                .ThenBy(d => d.DistrictName)
                .Select(d => new
                {
                    d.DistrictId,
                    d.DistrictName,
                    d.StateName,
                    d.Latitude,
                    d.Longitude
                })
                .ToListAsync();

            return Ok(districts);
        }

        // GET /api/dashboard/current/{districtId}
        [HttpGet("current/{districtId:int}")]
        public async Task<IActionResult> GetCurrent(int districtId)
        {
            var district = await _db.Districts.FindAsync(districtId);
            if (district == null)
                return NotFound(new { message = "District not found" });

            var weather = await _db.WeatherSnapshots
                .Where(w => w.DistrictId == districtId)
                .OrderByDescending(w => w.Timestamp)
                .FirstOrDefaultAsync();

            var aqi = await _db.AQISnapshots
                .Where(a => a.DistrictId == districtId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                district = new
                {
                    district.DistrictId,
                    district.DistrictName,
                    district.StateName
                },
                weather = weather == null ? null : new
                {
                    weather.TempCelsius,
                    weather.FeelsLikeCelsius,
                    weather.HumidityPct,
                    weather.WindSpeedMs,
                    weather.PressureHpa,
                    weather.ConditionText,
                    weather.ConditionIcon,
                    FetchedAt = weather.Timestamp
                },
                aqi = aqi == null ? null : new
                {
                    aqi.AQIValue,
                    aqi.Category,
                    aqi.PM25,
                    aqi.PM10,
                    aqi.NO2,
                    aqi.SO2,
                    aqi.CO,
                    aqi.O3,
                    FetchedAt = aqi.Timestamp
                }
            });
        }

        // GET /api/dashboard/daywise/{districtId}?days=7
        [HttpGet("daywise/{districtId:int}")]
        public async Task<IActionResult> GetDaywise(int districtId, [FromQuery] int days = 7)
        {
            if (days < 1 || days > 90)
                return BadRequest(new { message = "days must be between 1 and 90" });

            var district = await _db.Districts.FindAsync(districtId);
            if (district == null)
                return NotFound(new { message = "District not found" });

            var fromDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

            // Weather grouped by day
            var weatherDays = await _db.WeatherSnapshots
                .Where(w => w.DistrictId == districtId && w.Timestamp >= fromDate)
                .GroupBy(w => w.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgTempC = Math.Round(g.Average(w => (double?)w.TempCelsius ?? 0), 1),
                    MaxTempC = Math.Round(g.Max(w => (double?)w.TempCelsius ?? 0), 1),
                    MinTempC = Math.Round(g.Min(w => (double?)w.TempCelsius ?? 0), 1),
                    AvgHumidity = (int)g.Average(w => (double?)(int?)w.HumidityPct ?? 0),
                    AvgWindMs = Math.Round(g.Average(w => (double?)w.WindSpeedMs ?? 0), 2)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // AQI grouped by day
            var aqiDays = await _db.AQISnapshots
                .Where(a => a.DistrictId == districtId && a.Timestamp >= fromDate)
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgAQI = (int)g.Average(a => (double?)(int?)a.AQIValue ?? 0),
                    MaxAQI = g.Max(a => (int?)a.AQIValue ?? 0),
                    MinAQI = g.Min(a => (int?)a.AQIValue ?? 0),
                    AvgPM25 = Math.Round(g.Average(a => (double?)a.PM25 ?? 0), 2),
                    AvgPM10 = Math.Round(g.Average(a => (double?)a.PM10 ?? 0), 2)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Merge into continuous date series for Chart.js
            var dateRange = Enumerable.Range(0, days).Select(i => fromDate.AddDays(i)).ToList();
            var weatherMap = weatherDays.ToDictionary(x => x.Date);
            var aqiMap = aqiDays.ToDictionary(x => x.Date);

            var series = dateRange.Select(date =>
            {
                weatherMap.TryGetValue(date, out var w);
                aqiMap.TryGetValue(date, out var a);
                return new
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    AvgTempC = w?.AvgTempC,
                    MaxTempC = w?.MaxTempC,
                    MinTempC = w?.MinTempC,
                    AvgHumidity = w?.AvgHumidity,
                    AvgWindMs = w?.AvgWindMs,
                    AvgAQI = a?.AvgAQI,
                    MaxAQI = a?.MaxAQI,
                    MinAQI = a?.MinAQI,
                    AvgPM25 = a?.AvgPM25,
                    AvgPM10 = a?.AvgPM10,
                    Category = a != null && a.AvgAQI > 0 ? GetAQICategory(a.AvgAQI) : null
                };
            }).ToList();

            return Ok(new
            {
                district = new
                {
                    district.DistrictId,
                    district.DistrictName,
                    district.StateName
                },
                days,
                series
            });
        }

        private static string GetAQICategory(int aqi) => aqi switch
        {
            <= 50 => "Good",
            <= 100 => "Satisfactory",
            <= 200 => "Moderate",
            <= 300 => "Poor",
            <= 400 => "Very Poor",
            _ => "Severe"
        };
    }
}