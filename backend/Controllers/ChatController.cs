using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatRecommendationService _chatService;
    private readonly ApplicationDbContext _context;
    private readonly IConversationContextService _contextService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatRecommendationService chatService,
        IConversationContextService contextService,
        ApplicationDbContext context,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _contextService = contextService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message.Content)) return BadRequest("Message content cannot be empty.");

            // Get the user ID from the claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Failed to extract user ID from claims");
                return Unauthorized("Invalid authentication token.");
            }

            _logger.LogInformation("Processing chat message from user ID: {UserId}", userId);

            // Check if we need to start a new conversation
            if (!message.IsClarification && !message.IsFollowUp && string.IsNullOrEmpty(message.ConversationId))
            {
                // Start a new conversation session
                var session = await _contextService.StartNewSessionAsync(userId);
                message.ConversationId = session.Id.ToString();
            }

            // Process the message
            var chatMessage = new ChatMessage
            {
                Content = message.Content,
                Timestamp = DateTime.UtcNow,
                IsClarification = message.IsClarification,
                OriginalUserInput = message.OriginalUserInput,
                IsFollowUp = message.IsFollowUp,
                ConversationId = message.ConversationId
            };

            var response = await _chatService.ProcessMessageAsync(userId, chatMessage);

            // Check for null values to avoid NullReferenceException
            if (response == null) return StatusCode(500, "An error occurred while processing your message.");

            // Map the response to a DTO with null checks
            var responseDto = new ChatResponseDto
            {
                Message = response.Message,
                RecommendedVehicles = response.RecommendedVehicles,
                ClarificationNeeded = response.ClarificationNeeded,
                OriginalUserInput = response.OriginalUserInput,
                ConversationId = response.ConversationId,
                Parameters = response.UpdatedParameters != null
                    ? new RecommendationParametersDto
                    {
                        MinPrice = response.UpdatedParameters.MinPrice,
                        MaxPrice = response.UpdatedParameters.MaxPrice,
                        MinYear = response.UpdatedParameters.MinYear,
                        MaxYear = response.UpdatedParameters.MaxYear,
                        MaxMileage = response.UpdatedParameters.MaxMileage,
                        PreferredMakes = response.UpdatedParameters.PreferredMakes,
                        PreferredVehicleTypes = response.UpdatedParameters.PreferredVehicleTypes
                            ?.Select(t => t.ToString())?.ToList(),
                        PreferredFuelTypes = response.UpdatedParameters.PreferredFuelTypes?.Select(f => f.ToString())
                            ?.ToList(),
                        DesiredFeatures = response.UpdatedParameters.DesiredFeatures
                    }
                    : null
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, "An error occurred while processing your message.");
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetChatHistory([FromQuery] int limit = 10,
        [FromQuery] string conversationId = null)
    {
        try
        {
            // Get the user ID from the claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Failed to extract user ID from claims");
                return Unauthorized("Invalid authentication token.");
            }

            // Get chat history from database
            var query = _context.ChatHistory
                .Where(ch => ch.UserId == userId);

            // Filter by conversationId if provided
            if (!string.IsNullOrEmpty(conversationId) && int.TryParse(conversationId, out var convoId))
                query = query.Where(ch => ch.ConversationSessionId == convoId);

            var history = await query
                .OrderByDescending(ch => ch.Timestamp)
                .Take(limit)
                .Select(ch => new ChatHistoryDto
                {
                    Id = ch.Id,
                    UserMessage = ch.UserMessage,
                    AIResponse = ch.AIResponse,
                    Timestamp = ch.Timestamp.ToString("o"),
                    ConversationId = ch.ConversationSessionId.ToString()
                })
                .ToListAsync();

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history");
            return StatusCode(500, "An error occurred while retrieving chat history.");
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int limit = 5)
    {
        try
        {
            // Get the user ID from the claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid authentication token.");

            // Get recent conversations
            var conversations = await _context.ConversationSessions
                .Where(cs => cs.UserId == userId)
                .OrderByDescending(cs => cs.LastInteractionAt)
                .Take(limit)
                .Select(cs => new
                {
                    cs.Id,
                    cs.CreatedAt,
                    cs.LastInteractionAt,
                    MessageCount = _context.ChatHistory.Count(ch => ch.ConversationSessionId == cs.Id)
                })
                .ToListAsync();

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations");
            return StatusCode(500, "An error occurred while retrieving conversations.");
        }
    }

    [HttpPost("conversation/new")]
    public async Task<IActionResult> StartNewConversation()
    {
        try
        {
            // Get the user ID from the claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid authentication token.");

            // Create a new conversation session
            var session = await _contextService.StartNewSessionAsync(userId);

            return Ok(new { conversationId = session.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new conversation");
            return StatusCode(500, "An error occurred while starting a new conversation.");
        }
    }

    public class ChatMessageDto
    {
        public string Content { get; set; }
        public bool IsClarification { get; set; } = false;
        public string? OriginalUserInput { get; set; }
        public bool IsFollowUp { get; set; } = false;
        public string? ConversationId { get; set; }
    }

    public class ChatResponseDto
    {
        public string Message { get; set; }
        public List<Vehicle> RecommendedVehicles { get; set; } = new();
        public RecommendationParametersDto Parameters { get; set; }
        public bool ClarificationNeeded { get; set; }
        public string? OriginalUserInput { get; set; }
        public string? ConversationId { get; set; }
    }

    public class RecommendationParametersDto
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
        public int? MaxMileage { get; set; }
        public List<string> PreferredMakes { get; set; } = new();
        public List<string> PreferredVehicleTypes { get; set; } = new();
        public List<string> PreferredFuelTypes { get; set; } = new();
        public List<string> DesiredFeatures { get; set; } = new();
    }

    public class ChatHistoryDto
    {
        public int Id { get; set; }
        public string UserMessage { get; set; }
        public string AIResponse { get; set; }
        public string Timestamp { get; set; }
        public string? ConversationId { get; set; }
    }
}