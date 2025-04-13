using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication
    public class RecommendationsController(
        IAIRecommendationService recommendationService,
        ILogger<RecommendationsController> logger) : ControllerBase
    {
        private readonly ILogger<RecommendationsController> _logger = logger;
        private readonly IAIRecommendationService _recommendationService = recommendationService;

        [HttpGet]
        public async Task<IActionResult> GetRecommendations([FromQuery] RecommendationRequestModel request)
        {
            try
            {
                // Get user ID from claims
                Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated or invalid user ID");
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
                    _logger.LogInformation("Text prompt received: {TextPrompt}", request.TextPrompt);
                }

                // Get recommendations from service
                IEnumerable<Vehicle> recommendations =
                    await _recommendationService.GetRecommendationsAsync(userId, parameters);

                // Return recommendations
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, "An error occurred while getting recommendations");
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
                IEnumerable<Vehicle> recommendations = await _recommendationService.GetRecommendationsAsync(userId, parameters);

                // Return recommendations
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing recommendations");
                return StatusCode(500, "An error occurred while testing recommendations");
            }
        }
    }
}