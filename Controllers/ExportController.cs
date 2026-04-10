using EnviroWatch.Models;
using EnviroWatch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    public class ExportController : Controller
    {
        private readonly IExportService _exportService;
        private readonly AppDbContext _db;

        public ExportController(IExportService exportService, AppDbContext db)
        {
            _exportService = exportService;
            _db = db;
        }

        // GET: /export/csv/{districtId}?days=7
        [HttpGet("/export/csv/{districtId}")]
        public async Task<IActionResult> ExportCsv(int districtId, int days = 7)
        {
            var district = await _db.Districts.FindAsync(districtId);
            if (district == null) return NotFound();

            var data = await _exportService.ExportToCsvAsync(districtId, days);
            return File(data, "text/csv", $"EnviroWatch_{district.Name}_{days}days.csv");
        }

        // GET: /export/pdf/{districtId}?days=7
        [HttpGet("/export/pdf/{districtId}")]
        public async Task<IActionResult> ExportPdf(int districtId, int days = 7)
        {
            var district = await _db.Districts.FindAsync(districtId);
            if (district == null) return NotFound();

            var data = await _exportService.ExportToPdfAsync(districtId, days);
            return File(data, "application/pdf", $"EnviroWatch_{district.Name}_{days}days.pdf");
        }
    }
}
