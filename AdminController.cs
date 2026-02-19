using EnviroWatch.Data;
using EnviroWatch.Models;
using EnviroWatch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IDataFetchService _dataFetchService;

        public AdminController(AppDbContext context,
            IDataFetchService dataFetchService)
        {
            _context = context;
            _dataFetchService = dataFetchService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalDistricts = await _context.Districts.CountAsync(),
                TotalSubscriptions = await _context.Subscriptions.CountAsync(),
                TotalAlerts = await _context.AlertLogs.CountAsync(),
                RecentJobs = await _context.JobExecutionLogs
                    .OrderByDescending(j => j.StartTime)
                    .Take(10)
                    .ToListAsync()
            };
            return View(stats);
        }

        [HttpPost]
        public async Task<IActionResult> TriggerManualFetch()
        {
            await _dataFetchService.FetchAndStoreAllDistrictsDataAsync();
            return RedirectToAction("Index");
        }
    }
}
