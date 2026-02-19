using Microsoft.AspNetCore.Mvc;
using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Districts = await _context.Districts
                .OrderBy(d => d.StateName)
                .ThenBy(d => d.DistrictName)
                .ToListAsync();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
