// <copyright file="AuthController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Services;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService authService = authService;

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