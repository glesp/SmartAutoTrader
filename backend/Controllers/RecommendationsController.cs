// <copyright file="RecommendationsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.Controllers
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Services;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication
    public class RecommendationsController(
        IAIRecommendationService recommendationService,
        ILogger<RecommendationsController> logger) : ControllerBase
    {
        private readonly ILogger<RecommendationsController> logger = logger;
        private readonly IAIRecommendationService recommendationService = recommendationService;

        [HttpGet]
        public async Task<IActionResult> GetRecommendations([FromQuery] RecommendationRequestModel request)
        {
            try
            {
                // Get user ID from claims
                Claim? userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return this.Unauthorized("User not authenticated or invalid user ID");
                }

                // Convert request model to service parameters
                RecommendationParameters parameters = new()
                {
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    MinYear = request.MinYear,
                    MaxYear = request.MaxYear,
                    PreferredFuelTypes = request.FuelTypes ?? [],
                    PreferredVehicleTypes = request.VehicleTypes ?? [],
                    PreferredMakes = request.Makes ?? [],
                    DesiredFeatures = request.Features ?? [],

                    TextPrompt = request.TextPrompt,
                    MaxResults = request.MaxResults ?? 5,
                };

                // Log the text prompt for debugging
                if (!string.IsNullOrEmpty(request.TextPrompt))
                {
                    this.logger.LogInformation("Text prompt received: {TextPrompt}", request.TextPrompt);
                }

                // Get recommendations from service
                IEnumerable<Vehicle> recommendations =
                    await this.recommendationService.GetRecommendationsAsync(userId, parameters);

                // Return recommendations
                return this.Ok(recommendations);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error getting recommendations");
                return this.StatusCode(500, "An error occurred while getting recommendations");
            }
        }

        // Endpoint for testing without authentication
        [HttpGet("test/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> TestRecommendations(int userId, [FromQuery] RecommendationRequestModel request)
        {
            try
            {
                // Convert request model to service parameters
                RecommendationParameters parameters = new()
                {
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    MinYear = request.MinYear,
                    MaxYear = request.MaxYear,
                    PreferredFuelTypes = request.FuelTypes ?? [],
                    PreferredVehicleTypes = request.VehicleTypes ?? [],
                    PreferredMakes = request.Makes ?? [],
                    DesiredFeatures = request.Features ?? [],
                    TextPrompt = request.TextPrompt,
                    MaxResults = request.MaxResults ?? 5,
                };

                // Get recommendations from service
                IEnumerable<Vehicle> recommendations =
                    await this.recommendationService.GetRecommendationsAsync(userId, parameters);

                // Return recommendations
                return this.Ok(recommendations);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error testing recommendations");
                return this.StatusCode(500, "An error occurred while testing recommendations");
            }
        }
    }
}