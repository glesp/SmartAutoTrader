/* <copyright file="RecommendationsController.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the RecommendationsController class, which provides API endpoints for generating vehicle recommendations in the Smart Auto Trader application.
</summary>
<remarks>
The RecommendationsController class allows users to retrieve personalized vehicle recommendations based on various parameters such as price range, vehicle type, fuel type, and desired features. It leverages the IAIRecommendationService to generate recommendations using AI-based algorithms. The controller is secured with the [Authorize] attribute, ensuring only authenticated users can access its primary endpoints. A test endpoint is also provided for debugging purposes, which bypasses authentication.
</remarks>
<dependencies>
- System.Security.Claims
- Microsoft.AspNetCore.Authorization
- Microsoft.AspNetCore.Mvc
- SmartAutoTrader.API.Models
- SmartAutoTrader.API.Services
</dependencies>
 */

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

        /// <summary>
        /// Retrieves vehicle recommendations for a specific user without requiring authentication. For testing purposes only.
        /// </summary>
        /// <param name="userId">The ID of the user for whom recommendations are being generated.</param>
        /// <param name="request">The request model containing recommendation parameters such as price range, vehicle type, and desired features.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of recommended vehicles.</returns>
        /// <exception cref="Exception">Thrown if an error occurs while generating recommendations.</exception>
        /// <remarks>
        /// This endpoint bypasses authentication and is intended for testing purposes. It uses the IAIRecommendationService to generate recommendations based on the provided user ID and parameters.
        /// </remarks>
        /// <example>
        /// GET /api/Recommendations/test/1?MinPrice=10000&MaxPrice=50000&VehicleTypes=SUV&FuelTypes=Petrol.
        /// </example>
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