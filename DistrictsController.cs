using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnviroWatch.Models;
using EnviroWatch.Data;

namespace EnviroWatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistrictsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DistrictsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var districts = await _context.Districts
                .OrderBy(d => d.StateName)
                .ThenBy(d => d.DistrictName)
                .ToListAsync();
            return Ok(districts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var district = await _context.Districts.FindAsync(id);
            if (district == null) return NotFound();
            return Ok(district);
        }
    }
}
