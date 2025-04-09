using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Helpers;
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Services;

namespace SmartAutoTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(
        IChatRecommendationService chatService,
        IConversationContextService contextService,
        ApplicationDbContext context,
        ILogger<ChatController> logger) : ControllerBase
    {
        private readonly IChatRecommendationService _chatService = chatService;
        private readonly ApplicationDbContext _context = context;
        private readonly IConversationContextService _contextService = contextService;
        private readonly ILogger<ChatController> _logger = logger;

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    return BadRequest("Message content cannot be empty.");
                }

                int? userId = ClaimsHelper.GetUserIdFromClaims(User);
                if (userId is null)
                {
                    return Unauthorized();
                }

                _logger.LogInformation("Processing chat message from user ID: {UserId}", userId);

                // Check if we need to start a new conversation
                if (!message.IsClarification && !message.IsFollowUp && string.IsNullOrEmpty(message.ConversationId))
                {
                    // Start a new conversation session
                    ConversationSession session = await _contextService.StartNewSessionAsync((int)userId);
                    message.ConversationId = session.Id.ToString();
                }

                // Process the message
                ChatMessage chatMessage = new()
                {
                    Content = message.Content,
                    Timestamp = DateTime.UtcNow,
                    IsClarification = message.IsClarification,
                    OriginalUserInput = message.OriginalUserInput,
                    IsFollowUp = message.IsFollowUp,
                    ConversationId = message.ConversationId,
                };

                ChatResponse response = await _chatService.ProcessMessageAsync((int)userId, chatMessage);
                ChatResponseDto responseDto = new()
                {
                    Message = response.Message,
                    RecommendedVehicles = response.RecommendedVehicles,
                    ClarificationNeeded = response.ClarificationNeeded,
                    OriginalUserInput = response.OriginalUserInput,
                    ConversationId = response.ConversationId,
                    Parameters = response.UpdatedParameters is not null
                        ? new RecommendationParametersDto
                        {
                            MinPrice = response.UpdatedParameters.MinPrice,
                            MaxPrice = response.UpdatedParameters.MaxPrice,
                            MinYear = response.UpdatedParameters.MinYear,
                            MaxYear = response.UpdatedParameters.MaxYear,
                            MaxMileage = response.UpdatedParameters.MaxMileage,
                            PreferredMakes = response.UpdatedParameters.PreferredMakes ?? [],
                            PreferredVehicleTypes = response.UpdatedParameters.PreferredVehicleTypes?
                                .Select(t => t.ToString())
                                .ToList() ?? [],
                            PreferredFuelTypes = response.UpdatedParameters.PreferredFuelTypes?
                                .Select(f => f.ToString())
                                .ToList() ?? [],
                            DesiredFeatures = response.UpdatedParameters.DesiredFeatures ?? [],
                        }
                        : null,
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
        public async Task<IActionResult> GetChatHistory(
            [FromQuery] int limit = 10,
            [FromQuery] string conversationId = "")
        {
            try
            {
                int? userId = ClaimsHelper.GetUserIdFromClaims(User);
                if (userId is null)
                {
                    return Unauthorized();
                }

                // Get chat history from database
                IQueryable<ChatHistory> query = _context.ChatHistory
                    .Where(ch => ch.UserId == userId);

                // Filter by conversationId if provided
                if (!string.IsNullOrEmpty(conversationId) && int.TryParse(conversationId, out int convoId))
                {
                    query = query.Where(ch => ch.ConversationSessionId == convoId);
                }

                List<ChatHistoryDto> history = await query
                    .OrderByDescending(ch => ch.Timestamp)
                    .Take(limit)
                    .Select(ch => new ChatHistoryDto
                    {
                        Id = ch.Id,
                        UserMessage = ch.UserMessage,
                        AIResponse = ch.AIResponse,
                        Timestamp = ch.Timestamp.ToString("o"),
                        ConversationId = ch.ConversationSessionId.ToString(),
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
                int? userId = ClaimsHelper.GetUserIdFromClaims(User);
                if (userId is null)
                {
                    return Unauthorized();
                }

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
                        MessageCount = _context.ChatHistory.Count(ch => ch.ConversationSessionId == cs.Id),
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
                int? userId = ClaimsHelper.GetUserIdFromClaims(User);
                if (userId is null)
                {
                    return Unauthorized();
                }

                // Create a new conversation session
                ConversationSession session = await _contextService.StartNewSessionAsync((int)userId);

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
            public string? Content { get; set; }

            public bool IsClarification { get; set; }

            public string? OriginalUserInput { get; set; }

            public bool IsFollowUp { get; set; }

            public string? ConversationId { get; set; }
        }

        public class ChatResponseDto
        {
            public string? Message { get; set; }

            public List<Vehicle> RecommendedVehicles { get; set; } = [];

            public RecommendationParametersDto? Parameters { get; set; }

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

            public List<string> PreferredMakes { get; set; } = [];

            public List<string> PreferredVehicleTypes { get; set; } = [];

            public List<string> PreferredFuelTypes { get; set; } = [];

            public List<string> DesiredFeatures { get; set; } = [];
        }

        public class ChatHistoryDto
        {
            public int Id { get; set; }

            public string? UserMessage { get; set; }

            public string? AIResponse { get; set; }

            public string? Timestamp { get; set; }

            public string? ConversationId { get; set; }
        }
    }
}