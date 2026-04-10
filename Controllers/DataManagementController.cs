using EnviroWatch.Models;
using EnviroWatch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    [Authorize]
    public class DataManagementController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAQIService _aqiService;
        private readonly IWeatherService _weatherService;
        private readonly ILogger<DataManagementController> _logger;

        public DataManagementController(AppDbContext db, IAQIService aqiService, IWeatherService weatherService, ILogger<DataManagementController> logger)
        {
            _db = db;
            _aqiService = aqiService;
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var districts = await _db.Districts.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Districts = districts;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeedHistory(int? districtId)
        {
            try
            {
                var districts = districtId.HasValue 
                    ? await _db.Districts.Where(d => d.Id == districtId.Value).ToListAsync()
                    : await _db.Districts.ToListAsync();

                int totalAdded = 0;
                var start = DateTime.UtcNow.AddDays(-30);
                var end = DateTime.UtcNow;

                foreach (var district in districts)
                {
                    // Fetch AQI history (this is free/supported in AQIService)
                    var snapshots = await _aqiService.GetHistoricalFromApiAsync(district, start, end);
                    
                    if (snapshots.Count > 0)
                    {
                        // Filter out existing timestamps to avoid duplicates
                        var existingTimestamps = await _db.AQISnapshots
                            .Where(a => a.DistrictId == district.Id && a.RecordedAt >= start)
                            .Select(a => a.RecordedAt)
                            .ToListAsync();

                        var newSnapshots = snapshots
                            .Where(s => !existingTimestamps.Any(e => Math.Abs((e - s.RecordedAt).TotalMinutes) < 30))
                            .ToList();

                        _db.AQISnapshots.AddRange(newSnapshots);
                        
                        // For weather, if we don't have a historical API, we'll generate realistic 
                        // data points corresponding to the AQI snapshots to fill the dashboard.
                        // In a real production app, you'd use a paid Weather History API.
                        foreach (var aqi in newSnapshots)
                        {
                            var weather = new WeatherSnapshot
                            {
                                DistrictId = district.Id,
                                Temperature = 25 + new Random().Next(-5, 10), // Realistic range for India
                                Humidity = 50 + new Random().Next(-20, 30),
                                Pressure = 1010 + new Random().Next(-5, 5),
                                WindSpeed = 2 + new Random().NextDouble() * 5,
                                WeatherCondition = "Clear",
                                WeatherIcon = "01d",
                                Description = "Generated historical data",
                                RecordedAt = aqi.RecordedAt,
                                Sunrise = aqi.RecordedAt.Date.AddHours(6),
                                Sunset = aqi.RecordedAt.Date.AddHours(18)
                            };
                            _db.WeatherSnapshots.Add(weather);
                        }

                        totalAdded += newSnapshots.Count;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = $"Successfully added {totalAdded} historical data points.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed historical data");
                TempData["Error"] = "Failed to seed historical data: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> BulkInsert(int districtId, string csvData)
        {
            if (string.IsNullOrEmpty(csvData))
            {
                TempData["Error"] = "No data provided.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                int count = 0;
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 3) continue;

                    // Format: Date, AQI, Temp
                    if (DateTime.TryParse(parts[0], out DateTime date) && 
                        int.TryParse(parts[1], out int aqiVal) && 
                        double.TryParse(parts[2], out double temp))
                    {
                        _db.AQISnapshots.Add(new AQISnapshot
                        {
                            DistrictId = districtId,
                            AQI = aqiVal,
                            Category = GetAQICategory(aqiVal),
                            RecordedAt = date,
                            DominantPollutant = "PM2.5" // Default
                        });

                        _db.WeatherSnapshots.Add(new WeatherSnapshot
                        {
                            DistrictId = districtId,
                            Temperature = temp,
                            WeatherCondition = "Manual Entry",
                            RecordedAt = date,
                            Description = "Manually entered via bulk import"
                        });
                        count++;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = $"Successfully imported {count} records.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Import failed: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private string GetAQICategory(int aqi) => aqi switch
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
