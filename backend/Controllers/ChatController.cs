using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRecommendationService _chatService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatController> _logger;
        
        public ChatController(
            IChatRecommendationService chatService,
            ApplicationDbContext context,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _context = context;
            _logger = logger;
        }
        
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    return BadRequest(new { error = "Message content cannot be empty." });
                }
                
                // Get the user ID from the claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Failed to extract user ID from claims");
                    return BadRequest(new { error = "Invalid user identification." });
                }
                
                _logger.LogInformation("Processing chat message from user ID: {UserId}", userId);
                
                // Process the message
                var chatMessage = new ChatMessage
                {
                    Content = message.Content,
                    IsClarification = message.IsClarification,
                    OriginalUserInput = message.OriginalUserInput,
                    Timestamp = DateTime.UtcNow
                };
                
                var response = await _chatService.ProcessMessageAsync(userId, chatMessage);
                
                // Check for null values to avoid NullReferenceException
                if (response == null)
                {
                    return StatusCode(500, new { error = "Failed to process message" });
                }

                // Map the response to a DTO with null checks
                var responseDto = new ChatResponseDto
                {
                    Message = response.Message ?? "Sorry, I couldn't understand that.",
                    RecommendedVehicles = response.RecommendedVehicles,
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
                    },
                    ClarificationNeeded = response.ClarificationNeeded,
                    OriginalUserInput = response.OriginalUserInput
                };
                
                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { error = "An error occurred while processing your message. Please try again later." });
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
                    return BadRequest(new { error = "Invalid user identification." });
                }
                
                // Get actual chat history from database
                var chatHistory = await _context.ChatHistory
                    .Where(ch => ch.UserId == userId)
                    .OrderByDescending(ch => ch.Timestamp)
                    .Take(limit)
                    .Select(ch => new ChatHistoryDto
                    {
                        Id = ch.Id,
                        UserMessage = ch.UserMessage,
                        AIResponse = ch.AIResponse,
                        Timestamp = ch.Timestamp
                    })
                    .ToListAsync();

                return Ok(chatHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history");
                return StatusCode(500, new { error = "An error occurred while retrieving your chat history. Please try again later." });
            }
        }
    }
    
    public class ChatMessageDto
    {
        public string Content { get; set; }
        public bool IsClarification { get; set; } = false;
        public string? OriginalUserInput { get; set; } // Make nullable to avoid validation errors
    }
    
    public class ChatResponseDto
    {
        public string Message { get; set; }
        public List<Vehicle> RecommendedVehicles { get; set; } = new List<Vehicle>();
        public RecommendationParametersDto Parameters { get; set; }
        public bool ClarificationNeeded { get; set; }
        public string OriginalUserInput { get; set; }
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