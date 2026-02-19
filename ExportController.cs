using Microsoft.AspNetCore.Mvc;
using EnviroWatch.Services;

namespace EnviroWatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet("pdf/{districtId}/{days}")]
        public async Task<IActionResult> ExportPdf(int districtId, int days = 7)
        {
            try
            {
                var pdf = await _exportService.ExportToPdfAsync(districtId, days);
                return File(pdf, "application/pdf",
                    $"EnviroWatch_Report_{districtId}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("csv/{districtId}/{days}")]
        public async Task<IActionResult> ExportCsv(int districtId, int days = 7)
        {
            try
            {
                var csv = await _exportService.ExportToCsvAsync(districtId, days);
                return File(csv, "text/csv",
                    $"EnviroWatch_Data_{districtId}_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
