using EnviroWatch.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Controllers
{
    [Authorize]
    public class SubscriptionsController : Controller
    {
        private readonly AppDbContext _db;

        public SubscriptionsController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        // POST: /api/subscriptions — Create subscription
        [HttpPost("/api/subscriptions")]
        public async Task<IActionResult> Create([FromBody] SubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || request.DistrictId <= 0)
                return BadRequest(new { error = "Email and district are required" });

            // Check for existing subscription
            var existing = await _db.UserSubscriptions
                .FirstOrDefaultAsync(s => s.Email == request.Email && s.DistrictId == request.DistrictId);

            if (existing != null)
                return Conflict(new { error = "You are already subscribed to this city" });

            var subscription = new UserSubscription
            {
                Email = request.Email,
                DistrictId = request.DistrictId,
                AQIThreshold = request.AQIThreshold > 0 ? request.AQIThreshold : 200,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.UserSubscriptions.Add(subscription);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Subscribed successfully!", id = subscription.Id });
        }

        // GET: /api/subscriptions?email=...
        [HttpGet("/api/subscriptions")]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { error = "Email is required" });

            var subs = await _db.UserSubscriptions
                .Include(s => s.District)
                .Where(s => s.Email == email)
                .Select(s => new
                {
                    s.Id,
                    s.Email,
                    DistrictName = s.District!.Name,
                    s.DistrictId,
                    s.AQIThreshold,
                    s.IsActive,
                    CreatedAt = s.CreatedAt.ToString("dd MMM yyyy")
                })
                .ToListAsync();

            return Json(subs);
        }

        // DELETE: /api/subscriptions/{id}
        [HttpDelete("/api/subscriptions/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sub = await _db.UserSubscriptions.FindAsync(id);
            if (sub == null) return NotFound();

            _db.UserSubscriptions.Remove(sub);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Unsubscribed successfully" });
        }
    }

    public class SubscriptionRequest
    {
        public string Email { get; set; } = string.Empty;
        public int DistrictId { get; set; }
        public int AQIThreshold { get; set; }
    }
}
