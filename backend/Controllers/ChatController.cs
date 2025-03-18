using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRecommendationService _chatService;
        private readonly ILogger<ChatController> _logger;
        
        public ChatController(
            IChatRecommendationService chatService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }
        
        [HttpPost("message")]
public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
{
    try
    {
        if (string.IsNullOrWhiteSpace(message.Content))
        {
            return BadRequest("Message content cannot be empty");
        }
        
        // Get the user ID from the claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Failed to extract user ID from claims");
            return Unauthorized();
        }
        
        _logger.LogInformation("Processing chat message from user ID: {UserId}", userId);
        
        // Process the message
        var chatMessage = new ChatMessage
        {
            Content = message.Content,
            Timestamp = DateTime.UtcNow
        };
        
        var response = await _chatService.ProcessMessageAsync(userId, chatMessage);
        
        // Check for null values to avoid NullReferenceException
        if (response == null)
        {
            return StatusCode(500, "Failed to process message");
        }

        // Map the response to a DTO with null checks
        var responseDto = new ChatResponseDto
        {
            Message = response.Message ?? "Sorry, I couldn't understand that.",
            RecommendedVehicles = response.RecommendedVehicles?.Select(v => new VehicleDto
            {
                Id = v.Id,
                Make = v.Make ?? string.Empty,
                Model = v.Model ?? string.Empty,
                Year = v.Year,
                Price = v.Price,
                Mileage = v.Mileage,
                VehicleType = v.VehicleType.ToString(),
                FuelType = v.FuelType.ToString(),
                ImageUrl = v.Images?.FirstOrDefault()?.ImageUrl ?? "/assets/default-car.jpg"
            })?.ToList() ?? new List<VehicleDto>(),
            Parameters = new RecommendationParametersDto
            {
                MinPrice = response.UpdatedParameters?.MinPrice,
                MaxPrice = response.UpdatedParameters?.MaxPrice,
                MinYear = response.UpdatedParameters?.MinYear,
                MaxYear = response.UpdatedParameters?.MaxYear,
                PreferredMakes = response.UpdatedParameters?.PreferredMakes,
                PreferredVehicleTypes = response.UpdatedParameters?.PreferredVehicleTypes?.Select(t => t.ToString())?.ToList(),
                PreferredFuelTypes = response.UpdatedParameters?.PreferredFuelTypes?.Select(f => f.ToString())?.ToList(),
                DesiredFeatures = response.UpdatedParameters?.DesiredFeatures
            }
        };
        
        return Ok(responseDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing chat message");
        return StatusCode(500, "An error occurred while processing your message. Please try again later.");
    }
}
        
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] int limit = 10)
        {
            try
            {
                // Get the user ID from the claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from claims");
                    return Unauthorized();
                }
                
                // Since we don't have the repository pattern implemented yet, we'll return a placeholder
                // This should be replaced with actual database query
                
                var chatHistory = new List<ChatHistoryDto>
                {
                    new ChatHistoryDto
                    {
                        Id = 1,
                        UserMessage = "I'm looking for an SUV with good fuel economy.",
                        AIResponse = "Based on your preferences, I'd recommend looking at hybrid SUVs like the Toyota RAV4 Hybrid or Honda CR-V Hybrid. These offer excellent fuel economy while providing the space and versatility of an SUV.",
                        Timestamp = DateTime.UtcNow.AddHours(-1)
                    },
                    new ChatHistoryDto
                    {
                        Id = 2,
                        UserMessage = "What about electric options?",
                        AIResponse = "For electric SUVs, you might want to consider the Tesla Model Y, Ford Mustang Mach-E, or Volkswagen ID.4. These offer zero emissions and lower operating costs, though they typically have a higher upfront price.",
                        Timestamp = DateTime.UtcNow.AddMinutes(-30)
                    }
                };
                
                return Ok(chatHistory.Take(limit));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history");
                return StatusCode(500, "An error occurred while retrieving your chat history. Please try again later.");
            }
        }
    }
    
    public class ChatMessageDto
    {
        public string Content { get; set; }
    }
    
    public class ChatResponseDto
    {
        public string Message { get; set; }
        public List<VehicleDto> RecommendedVehicles { get; set; } = new List<VehicleDto>();
        public RecommendationParametersDto Parameters { get; set; }
    }
    
    public class VehicleDto
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public decimal Price { get; set; }
        public int Mileage { get; set; }
        public string VehicleType { get; set; }
        public string FuelType { get; set; }
        public string ImageUrl { get; set; }
    }
    
    public class RecommendationParametersDto
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
        public List<string> PreferredMakes { get; set; }
        public List<string> PreferredVehicleTypes { get; set; }
        public List<string> PreferredFuelTypes { get; set; }
        public List<string> DesiredFeatures { get; set; }
    }
    
    public class ChatHistoryDto
    {
        public int Id { get; set; }
        public string UserMessage { get; set; }
        public string AIResponse { get; set; }
        public DateTime Timestamp { get; set; }
    }
}