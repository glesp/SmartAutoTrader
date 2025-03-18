using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication
    public class RecommendationsController : ControllerBase
    {
        private readonly IAIRecommendationService _recommendationService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            IAIRecommendationService recommendationService,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations([FromQuery] RecommendationRequestModel request)
        {
            try
            {
                // Get user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("User not authenticated or invalid user ID");
                }

                // Convert request model to service parameters
                var parameters = new RecommendationParameters
                {
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    MinYear = request.MinYear,
                    MaxYear = request.MaxYear,
                    PreferredFuelTypes = request.FuelTypes,
                    PreferredVehicleTypes = request.VehicleTypes,
                    PreferredMakes = request.Makes,
                    DesiredFeatures = request.Features,
                    TextPrompt = request.TextPrompt, // Added text prompt
                    MaxResults = request.MaxResults ?? 5
                };

                // Log the text prompt for debugging
                if (!string.IsNullOrEmpty(request.TextPrompt))
                {
                    _logger.LogInformation("Text prompt received: {TextPrompt}", request.TextPrompt);
                }

                // Get recommendations from service
                var recommendations = await _recommendationService.GetRecommendationsAsync(userId, parameters);

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
                var parameters = new RecommendationParameters
                {
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    MinYear = request.MinYear,
                    MaxYear = request.MaxYear,
                    PreferredFuelTypes = request.FuelTypes,
                    PreferredVehicleTypes = request.VehicleTypes,
                    PreferredMakes = request.Makes,
                    DesiredFeatures = request.Features,
                    TextPrompt = request.TextPrompt, // Added text prompt
                    MaxResults = request.MaxResults ?? 5
                };

                // Get recommendations from service
                var recommendations = await _recommendationService.GetRecommendationsAsync(userId, parameters);

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

    public class RecommendationRequestModel
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
        public List<FuelType>? FuelTypes { get; set; }
        public List<VehicleType>? VehicleTypes { get; set; }
        public List<string>? Makes { get; set; }
        public List<string>? Features { get; set; }
        public string? TextPrompt { get; set; } // Added text prompt
        public int? MaxResults { get; set; }
    }
}