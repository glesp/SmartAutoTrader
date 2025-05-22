/* <copyright file="ConversationContextService.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ConversationContextService class, which manages conversation contexts and sessions for users in the Smart Auto Trader application.
</summary>
<remarks>
The ConversationContextService class is responsible for tracking and persisting conversation state across multiple user interactions. It integrates with the IChatRepository to manage conversation sessions and uses in-memory caching for performance optimization. The service ensures that conversation contexts are properly created, updated, and retrieved, while handling session timeouts and error logging.
</remarks>
<dependencies>
- System.Text.Json
- SmartAutoTrader.API.Models
- SmartAutoTrader.API.Repositories
- Microsoft.Extensions.Logging
</dependencies>
 */

namespace SmartAutoTrader.API.Services
{
    using System.Text.Json;
    using SmartAutoTrader.API.Models;
    using SmartAutoTrader.API.Repositories;

    /// <summary>
    /// Service responsible for managing conversation contexts and sessions for users.
    /// </summary>
    /// <remarks>
    /// This class tracks and persists conversation state across multiple user interactions, enabling contextual responses and dynamic query refinement.
    /// </remarks>
    public class ConversationContextService : IConversationContextService
    {
        private readonly Dictionary<int, ConversationContext> activeContexts = new();
        private readonly IChatRepository chatRepo;
        private readonly ILogger<ConversationContextService> logger;
        private readonly IUserRepository userRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationContextService"/> class.
        /// </summary>
        /// <param name="userRepo">The user repository for managing user data.</param>
        /// <param name="chatRepo">The chat repository for managing conversation sessions.</param>
        /// <param name="logger">The logger instance for logging errors and information.</param>
        public ConversationContextService(
            IUserRepository userRepo,
            IChatRepository chatRepo,
            ILogger<ConversationContextService> logger)
        {
            this.userRepo = userRepo;
            this.chatRepo = chatRepo;
            this.logger = logger;
        }

        /// <summary>
        /// Retrieves an existing conversation context for the specified user or creates a new one if none exists.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the <see cref="ConversationContext"/> for the user.
        /// </returns>
        /// <exception cref="JsonException">Thrown if deserialization of the session context fails.</exception>
        /// <remarks>
        /// This method checks for an active session and retrieves the associated context. If no session exists, a new context and session are created.
        /// </remarks>
        /// <example>
        /// <code>
        /// var context = await conversationContextService.GetOrCreateContextAsync(userId);
        /// Console.WriteLine($"Model used: {context.ModelUsed}");
        /// </code>
        /// </example>
        public async Task<ConversationContext> GetOrCreateContextAsync(int userId)
        {
            try
            {
                ConversationSession? session = await this.GetCurrentSessionAsync(userId);

                if (session != null)
                {
                    if (this.activeContexts.TryGetValue(userId, out ConversationContext? cachedContext))
                    {
                        return cachedContext;
                    }

                    if (!string.IsNullOrEmpty(session.SessionContext))
                    {
                        try
                        {
                            ConversationContext? context =
                                JsonSerializer.Deserialize<ConversationContext>(session.SessionContext);
                            if (context != null)
                            {
                                this.activeContexts[userId] = context;
                                return context;
                            }
                        }
                        catch (JsonException ex)
                        {
                            this.logger.LogError(ex, "Error deserializing conversation context for user {UserId}", userId);
                        }
                    }
                }

                ConversationContext newContext = new()
                {
                    LastInteraction = DateTime.UtcNow,
                };

                if (session == null)
                {
                    _ = await this.StartNewSessionAsync(userId);
                }

                string[] modelPool = { "fast", "refine", "clarify" };
                int index = new Random().Next(0, modelPool.Length);
                newContext.ModelUsed = modelPool[index];

                this.activeContexts[userId] = newContext;
                return newContext;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving conversation context for user {UserId}", userId);
                return new ConversationContext();
            }
        }

        /// <inheritdoc/>
        public async Task<ConversationContext> GetOrCreateContextAsync(int userId, int conversationId)
        {
            try
            {
                // Use the specific session ID, not just the most recent one
                ConversationSession? session = await this.chatRepo.GetSessionByIdAsync(conversationId, userId);

                if (session != null)
                {
                    if (this.activeContexts.TryGetValue(conversationId, out ConversationContext? cachedContext))
                    {
                        return cachedContext;
                    }

                    if (!string.IsNullOrEmpty(session.SessionContext))
                    {
                        try
                        {
                            ConversationContext? context =
                                JsonSerializer.Deserialize<ConversationContext>(session.SessionContext);
                            if (context != null)
                            {
                                this.activeContexts[conversationId] = context;
                                return context;
                            }
                        }
                        catch (JsonException ex)
                        {
                            this.logger.LogError(ex, "Error deserializing conversation context for user {UserId}, session {SessionId}", userId, conversationId);
                        }
                    }
                }

                // Create a new context if not found
                ConversationContext newContext = new()
                {
                    LastInteraction = DateTime.UtcNow,
                };

                // Optionally, start a new session if session == null
                if (session == null)
                {
                    _ = await this.StartNewSessionAsync(userId);
                }

                this.activeContexts[conversationId] = newContext;
                return newContext;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving conversation context for user {UserId}, session {SessionId}", userId, conversationId);
                return new ConversationContext();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateContextAsync(int userId, ConversationContext context)
        {
            try
            {
                this.activeContexts[userId] = context;
                context.LastInteraction = DateTime.UtcNow;

                ConversationSession session = await this.GetCurrentSessionAsync(userId) ??
                                              await this.StartNewSessionAsync(userId);

                session.SessionContext = JsonSerializer.Serialize(context);
                session.LastInteractionAt = DateTime.UtcNow;

                await this.chatRepo.UpdateSessionAsync(session);
                await this.chatRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating conversation context for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Starts a new conversation session for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the newly created <see cref="ConversationSession"/>.
        /// </returns>
        /// <remarks>
        /// This method creates a new session for the user, initializes the session context, and clears any cached context for the user.
        /// </remarks>
        /// <example>
        /// <code>
        /// var newSession = await conversationContextService.StartNewSessionAsync(userId);
        /// Console.WriteLine($"New session created with ID: {newSession.Id}");
        /// </code>
        /// </example>
        public async Task<ConversationSession> StartNewSessionAsync(int userId)
        {
            ConversationSession newSession = new()
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastInteractionAt = DateTime.UtcNow,
                SessionContext = JsonSerializer.Serialize(new ConversationContext()),
            };

            await this.chatRepo.AddSessionAsync(newSession);
            await this.chatRepo.SaveChangesAsync();

            _ = this.activeContexts.Remove(userId);

            return newSession;
        }

        /// <summary>
        /// Retrieves the current conversation session for the specified user, if one exists.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the current <see cref="ConversationSession"/> for the user, or null if no session exists.
        /// </returns>
        /// <remarks>
        /// This method retrieves the most recent session for the user within a specified timeout period.
        /// </remarks>
        /// <example>
        /// <code>
        /// var currentSession = await conversationContextService.GetCurrentSessionAsync(userId);
        /// if (currentSession != null)
        /// {
        ///     Console.WriteLine($"Current session ID: {currentSession.Id}");
        /// }
        /// </code>
        /// </example>
        public async Task<ConversationSession?> GetCurrentSessionAsync(int userId)
        {
            return await this.chatRepo.GetRecentSessionAsync(userId, TimeSpan.FromMinutes(30));
        }
    }
}