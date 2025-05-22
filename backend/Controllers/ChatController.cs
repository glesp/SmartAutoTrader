/* <copyright file="ChatController.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ChatController class, which provides API endpoints for managing chat interactions, retrieving chat history, and handling conversation sessions in the Smart Auto Trader application.
</summary>
<remarks>
The ChatController class is a key component of the backend API, enabling users to interact with the Smart Auto Trader assistant. It leverages dependency injection for services such as IChatRecommendationService, IConversationContextService, and ApplicationDbContext. The controller follows RESTful principles and includes endpoints for sending messages, retrieving chat history, managing conversations, and starting new conversation sessions.
</remarks>
<dependencies>
- Microsoft.AspNetCore.Authorization
- Microsoft.AspNetCore.Mvc
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.DTOs
- SmartAutoTrader.API.Helpers
- SmartAutoTrader.API.Models
- SmartAutoTrader.API.Repositories
- SmartAutoTrader.API.Services
</dependencies>
 */

namespace SmartAutoTrader.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.DTOs;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Repositories;
    using SmartAutoTrader.API.Services;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(
        IChatRecommendationService chatService,
        IConversationContextService contextService,
        ApplicationDbContext context,
        ILogger<ChatController> logger) : ControllerBase
    {
        private readonly IChatRecommendationService chatService = chatService;
        private readonly ApplicationDbContext context = context;
        private readonly IConversationContextService contextService = contextService;
        private readonly ILogger<ChatController> logger = logger;

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
        {
            try
            {
                // Explain: Log the ConversationId received directly from the frontend request DTO.
                this.logger.LogInformation(
                    "ChatController Received DTO: ConversationId='{ConversationId}', IsClarification={IsClarification}, IsFollowUp={IsFollowUp}",
                    message.ConversationId ?? "NULL",
                    message.IsClarification,
                    message.IsFollowUp);

                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    return this.BadRequest("Message content cannot be empty.");
                }

                int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
                if (userId is null)
                {
                    return this.Unauthorized();
                }

                this.logger.LogInformation("Processing chat message from user ID: {UserId}", userId);

                // Check if we need to start a new conversation
                if (!message.IsClarification && !message.IsFollowUp && string.IsNullOrEmpty(message.ConversationId))
                {
                    // Start a new conversation session
                    ConversationSession session = await this.contextService.StartNewSessionAsync((int)userId);
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

                ChatResponse response = await this.chatService.ProcessMessageAsync((int)userId, chatMessage);
                ChatResponseDto responseDto = new()
                {
                    Message = response.Message,
                    RecommendedVehicles = response.RecommendedVehicles,
                    ClarificationNeeded = response.ClarificationNeeded,
                    OriginalUserInput = response.OriginalUserInput,
                    ConversationId = response.ConversationId,
                    MatchedCategory = response.MatchedCategory,
                    Parameters = response.UpdatedParameters is not null
                        ? new RecommendationParametersDto
                        {
                            // Basic scalar parameters
                            MinPrice = response.UpdatedParameters.MinPrice,
                            MaxPrice = response.UpdatedParameters.MaxPrice,
                            MinYear = response.UpdatedParameters.MinYear,
                            MaxYear = response.UpdatedParameters.MaxYear,
                            MaxMileage = response.UpdatedParameters.MaxMileage,

                            // List parameters - preferred items
                            PreferredMakes = response.UpdatedParameters.PreferredMakes?.ToList() ?? [],
                            PreferredVehicleTypes = response.UpdatedParameters.PreferredVehicleTypes?
                                .Select(t => t.ToString())
                                .ToList() ?? [],
                            PreferredFuelTypes = response.UpdatedParameters.PreferredFuelTypes?
                                .Select(f => f.ToString())
                                .ToList() ?? [],
                            DesiredFeatures = response.UpdatedParameters.DesiredFeatures?.ToList() ?? [],

                            // List parameters - rejected/negated items
                            RejectedMakes = response.UpdatedParameters.RejectedMakes?.ToList() ??
                                           response.UpdatedParameters.ExplicitlyNegatedMakes?.ToList() ?? [],
                            RejectedVehicleTypes = response.UpdatedParameters.RejectedVehicleTypes?
                                .Select(t => t.ToString())
                                .ToList() ??
                                response.UpdatedParameters.ExplicitlyNegatedVehicleTypes?
                                .Select(t => t.ToString())
                                .ToList() ?? [],
                            RejectedFuelTypes = response.UpdatedParameters.RejectedFuelTypes?
                                .Select(f => f.ToString())
                                .ToList() ??
                                response.UpdatedParameters.ExplicitlyNegatedFuelTypes?
                                .Select(f => f.ToString())
                                .ToList() ?? [],
                            RejectedFeatures = response.UpdatedParameters.RejectedFeatures?.ToList() ?? [],

                            // Additional parameters
                            Transmission = response.UpdatedParameters.Transmission?.ToString(),
                            MinEngineSize = response.UpdatedParameters.MinEngineSize,
                            MaxEngineSize = response.UpdatedParameters.MaxEngineSize,
                            MinHorsePower = response.UpdatedParameters.MinHorsePower,
                            MaxHorsePower = response.UpdatedParameters.MaxHorsePower,
                            Intent = response.UpdatedParameters.Intent,
                        }
                        : null,
                };

                // Log retrieved history count
                this.logger.LogInformation(
                    "Retrieved {HistoryCount} history items for conversation {ConversationId}",
                    responseDto.RecommendedVehicles.Count,
                    message.ConversationId);

                this.logger.LogInformation(
                    "Processing response with {VehicleCount} vehicles for conversation {ConversationId}",
                    responseDto.RecommendedVehicles.Count,
                    message.ConversationId);

                return this.Ok(responseDto);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error processing chat message");
                return this.StatusCode(500, "An error occurred while processing your message.");
            }
        }

        /// <summary>
        /// Retrieves the chat history for the authenticated user.
        /// </summary>
        /// <param name="limit">The maximum number of chat history items to retrieve. Default is 10.</param>
        /// <param name="conversationId">The ID of the conversation to filter history by. Optional.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of chat history items.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="Exception">Thrown if an error occurs while retrieving chat history.</exception>
        /// <remarks>
        /// This method retrieves chat history from the database, optionally filtered by conversation ID. The history is ordered chronologically.
        /// </remarks>
        /// <example>
        /// GET /api/chat/history?limit=5&conversationId=123.
        /// </example>
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory(
            [FromQuery] int limit = 10,
            [FromQuery] string conversationId = "")
        {
            try
            {
                int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
                if (userId is null)
                {
                    return this.Unauthorized();
                }

                // Get chat history from database
                IQueryable<ChatHistory> query = this.context.ChatHistory
                    .Where(ch => ch.UserId == userId);

                // Filter by conversationId if provided
                if (!string.IsNullOrEmpty(conversationId) && int.TryParse(conversationId, out int convoId))
                {
                    query = query.Where(ch => ch.ConversationSessionId == convoId);
                }

                // Make sure history is in chronological order when passed to ProcessMessageAsync
                List<ChatHistoryDto> history = await query
                    .OrderBy(ch => ch.Timestamp) // Change to OrderBy instead of OrderByDescending
                    .Take(limit)
                    .Select(
                        ch => new ChatHistoryDto
                        {
                            Id = ch.Id,
                            UserMessage = ch.UserMessage,
                            AIResponse = ch.AIResponse,
                            Timestamp = ch.Timestamp.ToString("o"),
                            ConversationId = ch.ConversationSessionId.ToString(),
                        })
                    .ToListAsync();

                return this.Ok(history);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving chat history");
                return this.StatusCode(500, "An error occurred while retrieving chat history.");
            }
        }

        /// <summary>
        /// Retrieves a list of recent conversations for the authenticated user.
        /// </summary>
        /// <param name="limit">The maximum number of conversations to retrieve. Default is 5.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of recent conversations.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="Exception">Thrown if an error occurs while retrieving conversations.</exception>
        /// <remarks>
        /// This method retrieves recent conversations from the database, ordered by the last interaction timestamp.
        /// </remarks>
        /// <example>
        /// GET /api/chat/conversations?limit=3.
        /// </example>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] int limit = 5)
        {
            try
            {
                int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
                if (userId is null)
                {
                    return this.Unauthorized();
                }

                // Get recent conversations
                var conversations = await this.context.ConversationSessions
                    .Where(cs => cs.UserId == userId)
                    .OrderByDescending(cs => cs.LastInteractionAt)
                    .Take(limit)
                    .Select(
                        cs => new
                        {
                            cs.Id,
                            cs.CreatedAt,
                            cs.LastInteractionAt,
                            MessageCount = this.context.ChatHistory.Count(ch => ch.ConversationSessionId == cs.Id),
                        })
                    .ToListAsync();

                return this.Ok(conversations);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving conversations");
                return this.StatusCode(500, "An error occurred while retrieving conversations.");
            }
        }

        /// <summary>
        /// Starts a new conversation session for the authenticated user.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the new conversation ID and a welcome message.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="Exception">Thrown if an error occurs while starting a new conversation.</exception>
        /// <remarks>
        /// This method creates a new conversation session in the database and returns a standard welcome message to the user.
        /// </remarks>
        /// <example>
        /// POST /api/chat/conversation/new
        /// Response: { "conversationId": "456", "welcomeMessage": "Hi! I'm your Smart Auto Trader assistant..." }.
        /// </example>
        [HttpPost("conversation/new")]
        public async Task<IActionResult> StartNewConversation()
        {
            try
            {
                int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
                if (userId is null)
                {
                    return this.Unauthorized();
                }

                // Create a new conversation session
                ConversationSession session = await this.contextService.StartNewSessionAsync((int)userId);

                // Define the standard welcome message
                string welcomeMessage = "Hi! I'm your Smart Auto Trader assistant. I can help you find the perfect car. What are you looking for today?";

                // Return the new conversation ID and the welcome message
                return this.Ok(new { conversationId = session.Id.ToString(), welcomeMessage = welcomeMessage });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error starting new conversation");
                return this.StatusCode(500, "An error occurred while starting a new conversation.");
            }
        }
    }
}