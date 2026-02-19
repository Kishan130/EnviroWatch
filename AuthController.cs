using Microsoft.AspNetCore.Mvc;
using EnviroWatch.Services;

namespace EnviroWatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _authService.AuthenticateAsync(
                    request.Email,
                    request.Name,
                    request.Provider,
                    request.ProviderId);

                return Ok(new { token, email = request.Email });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
    }
}
