using Microsoft.AspNetCore.Mvc;
using EnviroWatch.Services;

namespace EnviroWatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AQIController : ControllerBase
    {
        private readonly IAQIService _aqiService;

        public AQIController(IAQIService aqiService)
        {
            _aqiService = aqiService;
        }

        [HttpGet("current/{districtId}")]
        public async Task<IActionResult> GetCurrent(int districtId)
        {
            var data = await _aqiService.GetCurrentAQIAsync(districtId);
            if (data == null) return NotFound();
            return Ok(new
            {
                data,
                category = _aqiService.GetAQICategory(data.AQI),
                color = _aqiService.GetAQIColor(data.AQI)
            });
        }

        [HttpGet("historical/{districtId}/{days}")]
        public async Task<IActionResult> GetHistorical(int districtId, int days)
        {
            var data = await _aqiService.GetHistoricalAQIAsync(districtId, days);
            return Ok(data);
        }
    }
}
