using EnviroWatch.Models;
using EnviroWatch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWeatherService _weatherService;
        private readonly IAQIService _aqiService;
        private readonly IRecommendationService _recommendationService;

        public HomeController(AppDbContext db, IWeatherService weatherService, IAQIService aqiService, IRecommendationService recommendationService)
        {
            _db = db;
            _weatherService = weatherService;
            _aqiService = aqiService;
            _recommendationService = recommendationService;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /api/districts — Return all districts for map markers
        [HttpGet("/api/districts")]
        public async Task<IActionResult> GetDistricts()
        {
            var districts = await _db.Districts
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.State,
                    d.Latitude,
                    d.Longitude,
                    d.IsMetroCity
                })
                .ToListAsync();

            return Json(districts);
        }

        // GET: /api/district/{id}/live — Get live weather + AQI + recommendations for a city
        [HttpGet("/api/district/{id}/live")]
        public async Task<IActionResult> GetLiveData(int id)
        {
            var district = await _db.Districts.FindAsync(id);
            if (district == null) return NotFound(new { error = "District not found" });

            var weatherData = await _weatherService.GetCurrentWeatherAsync(district);
            var aqiData = await _aqiService.GetCurrentAQIAsync(district);

            HealthRecommendation? recommendation = null;
            if (weatherData != null && aqiData != null)
            {
                recommendation = _recommendationService.GetRecommendation(
                    aqiData.AQI, aqiData.Category, weatherData.Temperature, weatherData.Humidity,
                    weatherData.WindSpeed, weatherData.WeatherCondition);
            }

            return Json(new
            {
                district = new { id = district.Id, name = district.Name, state = district.State },
                weather = weatherData != null ? new
                {
                    temperature = weatherData.Temperature,
                    feelsLike = weatherData.FeelsLike,
                    humidity = weatherData.Humidity,
                    pressure = weatherData.Pressure,
                    windSpeed = weatherData.WindSpeed,
                    windDirection = weatherData.WindDirection,
                    visibility = weatherData.Visibility,
                    cloudCover = weatherData.CloudCover,
                    weatherCondition = weatherData.WeatherCondition,
                    weatherIcon = weatherData.WeatherIcon,
                    description = weatherData.Description,
                    sunrise = TimeZoneInfo.ConvertTimeFromUtc(weatherData.Sunrise,
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("hh:mm tt") + " IST",
                    sunset = TimeZoneInfo.ConvertTimeFromUtc(weatherData.Sunset,
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("hh:mm tt") + " IST"
                } : null,
                aqi = aqiData != null ? new
                {
                    aqi = aqiData.AQI,
                    category = aqiData.Category,
                    pm25 = aqiData.PM25,
                    pm10 = aqiData.PM10,
                    o3 = aqiData.O3,
                    no2 = aqiData.NO2,
                    so2 = aqiData.SO2,
                    co = aqiData.CO,
                    dominantPollutant = aqiData.DominantPollutant
                } : null,
                recommendation = recommendation != null ? new
                {
                    verdict = recommendation.Verdict,
                    verdictClass = recommendation.VerdictClass,
                    aqiAdvice = recommendation.AQIAdvice,
                    weatherAdvice = recommendation.WeatherAdvice,
                    dosAndDonts = recommendation.DosAndDonts,
                    heatIndex = recommendation.HeatIndex,
                    heatIndexCategory = recommendation.HeatIndexCategory,
                    overallSummary = recommendation.OverallSummary
                } : null
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
