namespace SmartAutoTrader.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Services;

    /// <summary>
    /// Controller for handling user authentication operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService authService = authService;

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="model">Registration information containing username, email, password and personal details.</param>
        /// <returns>User information and success message upon successful registration.</returns>
        /// <response code="200">Returns the user information and confirmation message.</response>
        /// <response code="400">If the registration fails (e.g. email already exists).</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                User user = await this.authService.RegisterAsync(
                    model.Username,
                    model.Email,
                    model.Password,
                    model.FirstName,
                    model.LastName,
                    model.PhoneNumber);

                return this.Ok(
                    new
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
                return this.BadRequest(new { ex.Message });
            }
        }

        /// <summary>
        /// Authenticates a user and provides a JWT token.
        /// </summary>
        /// <param name="model">Login credentials containing email and password.</param>
        /// <returns>JWT token and user information upon successful authentication.</returns>
        /// <response code="200">Returns the token, user information and confirmation message.</response>
        /// <response code="400">If the authentication fails (e.g. invalid credentials).</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                (string token, User user) = await this.authService.LoginAsync(model.Email, model.Password);

                return this.Ok(
                    new
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
                return this.BadRequest(new { ex.Message });
            }
        }
    }
}