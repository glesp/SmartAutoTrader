// <copyright file="ChatController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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

                return this.Ok(new { conversationId = session.Id });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error starting new conversation");
                return this.StatusCode(500, "An error occurred while starting a new conversation.");
            }
        }
    }
}