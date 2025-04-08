using Microsoft.AspNetCore.Mvc;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                User user = await _authService.RegisterAsync(
                    model.Username,
                    model.Email,
                    model.Password,
                    model.FirstName,
                    model.LastName,
                    model.PhoneNumber);

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    Message = "Registration successful",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                (string token, User user) = await _authService.LoginAsync(model.Email, model.Password);

                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                    },
                    Message = "Login successful",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }
    }
}