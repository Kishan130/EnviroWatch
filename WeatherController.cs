using Microsoft.AspNetCore.Mvc;
using EnviroWatch.Services;

namespace EnviroWatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("current/{districtId}")]
        public async Task<IActionResult> GetCurrent(int districtId)
        {
            var data = await _weatherService.GetCurrentWeatherAsync(districtId);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpGet("historical/{districtId}/{days}")]
        public async Task<IActionResult> GetHistorical(int districtId, int days)
        {
            var data = await _weatherService.GetHistoricalWeatherAsync(
                districtId, days);
            return Ok(data);
        }
    }
}
