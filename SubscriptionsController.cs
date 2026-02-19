using Microsoft.AspNetCore.Mvc;
using EnviroWatch.Services;
using System.Security.Claims;

namespace EnviroWatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscription(
            [FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                var subscription = await _subscriptionService
                    .CreateSubscriptionAsync(
                        request.UserId, request.DistrictId, request.Threshold);
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserSubscriptions(int userId)
        {
            var subscriptions = await _subscriptionService
                .GetUserSubscriptionsAsync(userId);
            return Ok(subscriptions);
        }

        [HttpDelete("{subscriptionId}/{userId}")]
        public async Task<IActionResult> DeleteSubscription(
            int subscriptionId, int userId)
        {
            var result = await _subscriptionService
                .DeleteSubscriptionAsync(subscriptionId, userId);
            if (result) return Ok();
            return NotFound();
        }
    }

    public class CreateSubscriptionRequest
    {
        public int UserId { get; set; }
        public int DistrictId { get; set; }
        public int Threshold { get; set; } = 200;
    }
}
